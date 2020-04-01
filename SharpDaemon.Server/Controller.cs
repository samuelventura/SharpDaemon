using System;
using System.Collections.Generic;

namespace SharpDaemon.Server
{
    public class Controller : Disposable, IScriptable
    {
        private readonly int delay;
        private readonly Output output;
        private readonly Runner runner;
        private readonly Action<DaemonLog> logger;
        private readonly Action<Exception> handler;
        private readonly Dictionary<string, DaemonRT> daemons;

        public class Args
        {
            public Output Output { get; set; }
            public int RestartDelay { get; set; }
            public Action<DaemonLog> DaemonLogger { get; set; }
            public Action<Exception> ExceptionHandler { get; set; }
        }

        public Controller(Args args)
        {
            delay = args.RestartDelay;
            logger = args.DaemonLogger;
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
                        named.Output("Daemon starting... {0}|{1}|{2}", dto.Id, dto.Path, dto.Args);
                        var rt = DoStart(dto);
                        named.Output("Daemon started {0} {1} {2}", rt.Dto.Id, rt.Process.Name, rt.Process.Id);
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
                var process = new DaemonProcess(new DaemonProcess.Args
                {
                    Executable = dto.Path,
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
                    Logger = logger,
                    Process = process,
                    Runner = runner,
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
        public DateTime? Restart;
        public Action<DaemonLog> Logger;
        public DaemonProcess Process;
        public Runner Runner;

        protected override void Dispose(bool disposed)
        {
            Tools.Try(Process.Dispose);
            Tools.Try(Runner.Dispose);
        }

        public void ReadLoop()
        {
            var line = Process.ReadLine();
            while (!string.IsNullOrWhiteSpace(line))
            {
                if (line.StartsWith("#")) ParseAndLog(line);
                line = Process.ReadLine();
            }
        }

        private void ParseAndLog(string line)
        {
            var log = new DaemonLog()
            {
                Id = Dto.Id,
                Pid = Process.Id,
                Name = Process.Name,
            };
            Log.Parse(log, line);
            Logger(log);
        }
    }

    public class DaemonLog : Log
    {
        public string Id { get; set; }
        public int Pid { get; set; }
        public string Name { get; set; }
    }
}
