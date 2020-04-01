using System;
using System.IO;
using System.Collections.Generic;

namespace SharpDaemon.Server
{
    public class Controller : Disposable, IScriptable
    {
        private readonly int delay;
        private readonly string root;
        private readonly Runner runner;
        private readonly Output output;
        private readonly Action<Exception> handler;
        private readonly Dictionary<string, DaemonRT> daemons;

        public class Args
        {
            public string Root { get; set; }
            public Output Output { get; set; }
            public int RestartDelay { get; set; }
            public Action<Exception> ExceptionHandler { get; set; }
        }

        public Controller(Args args)
        {
            root = args.Root;
            delay = args.RestartDelay;
            handler = args.ExceptionHandler;
            output = new NamedOutput("CONTROLLER", args.Output);
            daemons = new Dictionary<string, DaemonRT>();
            runner = new Runner(new Runner.Args
            {
                ThreadName = "Controller",
                ExceptionHandler = handler,
                IdleAction = IdleLoop,
                IdleDelay = 1,
            });
        }

        public void Execute(Output output, params string[] tokens)
        {
            if (tokens[0] == "daemon")
            {
                var named = new NamedOutput("CONTROLLER", output);
                if ((tokens.Length == 4 || tokens.Length == 5) && tokens[1] == "install")
                {
                    runner.Run(() =>
                    {
                        var dto = new DaemonDto
                        {
                            Id = tokens[2],
                            Path = tokens[3],
                            Args = tokens.Length > 4 ? tokens[4] : string.Empty,
                        };
                        var id = dto.Id;
                        if (daemons.ContainsKey(id))
                        {
                            named.Output("Daemon {0} already installed", id);
                            return;
                        }
                        named.Output("Daemon {0} starting... {1}|{2}", dto.Id, dto.Path, dto.Args);
                        var rt = DoStart(dto);
                        named.Output("Daemon {0} started {1} {2}", rt.Dto.Id, rt.Process.Id, rt.Process.Name);
                    }, named.OnException);
                }
                if (tokens.Length == 3 && tokens[1] == "uninstall")
                {
                    runner.Run(() =>
                    {
                        var id = tokens[2];
                        if (!daemons.ContainsKey(id))
                        {
                            named.Output("Daemon {0} is not installed", id);
                            return;
                        }
                        Tools.Try(daemons[id].Dispose);
                        daemons.Remove(id);
                        named.Output("Daemon {0} stopped", id);
                    }, named.OnException);
                }
                if (tokens.Length == 2 && tokens[1] == "status")
                {
                    runner.Run(() =>
                    {
                        named.Output("Id|Pid|Name|Status");
                        foreach (var rt in daemons.Values)
                        {
                            named.Output("{0}|{1}|{2}|{3}", rt.Dto.Id, rt.Process.Id, rt.Process.Name, rt.Status);
                        }
                        named.Output("{0} daemon(s)", daemons.Count);
                    }, named.OnException);
                }
            }
        }

        protected override void Dispose(bool disposed)
        {
            runner.Dispose(() =>
            {
                foreach (var rt in daemons.Values) Tools.Try(rt.Dispose);
                daemons.Clear();
            });
        }

        //in process runner
        private void Restart(DaemonRT rt)
        {
            runner.Run(() =>
            {
                var id = rt.Dto.Id;
                rt.Restart = DateTime.Now.AddMilliseconds(delay);
                rt.Status = "Restarting...";
                Tools.Try(rt.Dispose);
            });
        }

        //in controller runner
        private void IdleLoop()
        {
            var restart = new List<DaemonRT>();
            foreach (var rt in daemons.Values)
            {
                if (rt.Disposed
                    && rt.Restart.HasValue
                    && DateTime.Now > rt.Restart.Value)
                    restart.Add(rt);
            }
            foreach (var rt in restart)
            {
                var id = rt.Dto.Id;
                output.Output("Daemon {0} restarting after {1}ms", id, delay);
                daemons.Remove(id);
                DoStart(rt.Dto);
            }
        }

        private DaemonRT DoStart(DaemonDto dto)
        {
            using (var disposer = new Disposer())
            {
                var path = dto.Path; //fix paths relative to root (downloads)
                if (!Path.IsPathRooted(path)) path = Path.GetFullPath(Path.Combine(root, path));
                var process = new DaemonProcess(new DaemonProcess.Args
                {
                    Executable = path,
                    Arguments = dto.Args,
                    ExceptionHandler = handler,
                });
                disposer.Push(process);
                var runner = new Runner(new Runner.Args
                {
                    ThreadName = dto.Id,
                    ExceptionHandler = handler,
                });
                disposer.Push(runner);
                var rt = new DaemonRT
                {
                    Dto = dto,
                    Process = process,
                    Runner = runner,
                    Output = output,
                    Status = "Starting...",
                };
                //ensure cleanup order
                disposer.Push(rt);
                daemons[dto.Id] = rt;
                runner.Run(rt.ReadLoop);
                runner.Run(() => Restart(rt));
                disposer.Clear();
                return rt;
            }
        }
    }

    class DaemonRT : Disposable
    {
        public DaemonDto Dto;
        public Output Output;
        public Runner Runner;
        public DateTime? Restart;
        public DaemonProcess Process;
        public volatile string Status;

        protected override void Dispose(bool disposed)
        {
            Tools.Try(Process.Dispose);
            Tools.Try(Runner.Dispose);
        }

        public void ReadLoop()
        {
            var line = Process.ReadLine();
            while (line != null)
            {
                Output.Output("Daemon {0} {1} < {2}", Dto.Id, Process.Id, line);
                Status = line.Split(new char[] { '\n' }, 2)[0];
                if (line.StartsWith("!")) return;
                line = Process.ReadLine();
            }
        }
    }
}
