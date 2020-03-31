using System;
using System.Threading;
using System.Collections.Generic;

namespace SharpDaemon.Server
{
    public class Controller : IDisposable, IScriptable
    {
        private readonly int delay;
        private readonly Runner runner;
        private readonly Output output;
        private readonly Action<DaemonLog> logger;
        private readonly Action<Exception> handler;
        private readonly Dictionary<string, DaemonRT> daemons;

        public class Args
        {
            public int RestartDelay { get; set; }
            public Output Output { get; set; }
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
            runner = new Runner(new Runner.Args { ExceptionHandler = handler });
        }

        public void Dispose()
        {
            runner.Dispose(() =>
            {
                foreach (var rt in daemons.Values)
                {
                    Tools.Try(rt.Process.Dispose, handler);
                    Tools.Try(rt.Runner.Dispose, handler);
                }
                daemons.Clear();
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
                        Tools.Assert(!daemons.ContainsKey(dto.Id), "Daemon {0} already installed", dto.Id);
                        named.Output("Process starting... {0}|{1}|{2}", dto.Id, dto.Path, dto.Args);
                        var rt = DoStart(dto);
                        named.Output("Process started {0} {1}", rt.Process.Name, rt.Process.Id);
                    }, named.OnException);
                }
                if (tokens.Length == 3 && tokens[1] == "uninstall")
                {
                    runner.Run(() =>
                    {
                        var id = tokens[2];
                        Tools.Assert(daemons.ContainsKey(id), "Daemon {0} is not installed", id);
                        DoStop(id);
                    }, named.OnException);
                }
            }
        }

        private void Restart(DaemonDto dto)
        {
            output.Output("Daemon {0} restarting in {1}ms", dto.Id, delay);
            Thread.Sleep(delay);
            runner.Run(() =>
            {
                var id = dto.Id;
                Tools.Assert(daemons.ContainsKey(id), "Daemon {0} is not installed", id);
                DoStop(id);
                DoStart(dto);
            });
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
                var runner = new Runner(new Runner.Args { ExceptionHandler = handler });
                disposer.Push(runner);
                var rt = new DaemonRT()
                {
                    Id = dto.Id,
                    Path = dto.Path,
                    Args = dto.Args,
                    Logger = logger,
                    Handler = handler,
                    Restart = Restart,
                    Process = process,
                    Runner = runner,
                };
                daemons[dto.Id] = rt;
                //runner wont exit otherwise
                disposer.Push(process);
                runner.Run(rt.ReadLoop);
                disposer.Clear();
                return rt;
            }
        }

        private void DoStop(string id)
        {
            var rt = daemons[id];
            daemons.Remove(id);
            Tools.Try(rt.Process.Dispose, handler);
            Tools.Try(rt.Runner.Dispose, handler);
        }
    }

    public class DaemonRT : DaemonDto
    {
        public Action<DaemonDto> Restart { get; set; }
        public Action<DaemonLog> Logger { get; set; }
        public Action<Exception> Handler { get; set; }
        public Runner Runner { get; set; }
        public DaemonProcess Process { get; set; }

        public void ReadLoop()
        {
            Tools.Try(TryLoop, Handler);
            Restart(this);
        }

        private void TryLoop()
        {
            var line = Process.ReadLine();
            while (!string.IsNullOrWhiteSpace(line))
            {
                if (line.StartsWith("#"))
                {
                    Tools.Try(() => ParseAndLog(line), Handler);
                }
                line = Process.ReadLine();
            }
        }

        private void ParseAndLog(string line)
        {
            var log = new DaemonLog()
            {
                Uid = Id,
                Pid = Process.Id,
                Name = Process.Name,
            };
            Log.Parse(log, line);
            Logger(log);
        }
    }

    public class DaemonLog : Log
    {
        public int Pid { get; set; }
        public string Uid { get; set; }
        public string Name { get; set; }
    }
}
