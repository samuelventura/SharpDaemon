using System;
using System.Collections.Generic;

namespace SharpDaemon.Server
{
    public partial class Controller : Disposable
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
            //ensure restart one-by-one strategy works
            delay = Math.Max(100, args.RestartDelay);
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
                rt.UpdateRestart(delay);
                rt.UpdateStatus("Restarting...");
                Tools.Try(rt.Dispose);
            });
        }

        //in controller runner
        private void IdleLoop()
        {
            foreach (var rt in daemons.Values)
            {
                if (rt.Disposed && rt.NeedRestart())
                {
                    var id = rt.Id;
                    //reschedule in case contructor throws
                    rt.UpdateRestart(delay); //min=100ms
                    output.Output("Daemon {0} restarting after {1}ms", id, delay);
                    var nrt = new DaemonRT(rt.Dto, root, output, handler);
                    daemons[id] = nrt;
                    nrt.Run(() => Restart(nrt));
                    output.Output("Daemon {0} restarted {1}", id, nrt.ProcessInfo());
                    //one at the time to prevent one throwing from stopping all
                    //needs restart-delay >> idle-delay to get to process latest
                    break; //process only one
                }
            }
        }
    }
}
