using System;

namespace SharpDaemon.Server
{
    public class Instance : IDisposable
    {
        private readonly ShellFactory factory;
        private readonly Listener listener;
        private readonly Manager manager;
        private readonly Outputs outputs;
        private readonly int port;

        public class Args
        {
            public int RestartDelay { get; set; }
            public int TcpPort { get; set; }
            public string DbPath { get; set; }
            public string Downloads { get; set; }
            public Outputs Outputs { get; set; }
        }

        public Instance(Args args)
        {
            outputs = args.Outputs;
            using (var disposer = new Disposer())
            {
                outputs.Output("DbPath {0}", args.DbPath);
                outputs.Output("Downloads {0}", args.Downloads);
                outputs.Output("RestartDelay {0}", args.RestartDelay);
                outputs.Output("TcpPort {0}", args.TcpPort);
                factory = new ShellFactory();
                var controller = new Controller(new Controller.Args
                {
                    ExceptionHandler = OnException,
                    DaemonLogger = OnDaemonLog,
                    RestartDelay = args.RestartDelay,
                });
                disposer.Push(controller);
                manager = new Manager(new Manager.Args
                {
                    ExceptionHandler = OnException,
                    DatabasePath = args.DbPath,
                    Downloads = args.Downloads,
                    Controller = controller,
                });
                disposer.Push(manager);
                //load from database
                manager.Start(outputs);
                listener = new Listener(new Listener.Args
                {
                    ExceptionHandler = OnException,
                    ShellFactory = factory,
                    TcpPort = args.TcpPort,
                    Output = outputs,
                });
                disposer.Push(listener);
                port = listener.Port;
                factory.Add(listener);
                factory.Add(manager);
                //fill factory before start
                listener.Start();
                disposer.Clear();
            }
        }

        public int Port { get { return port; } }

        public Shell CreateShell() { return factory.Create(); }

        public void Dispose()
        {
            Tools.Try(listener.Dispose, OnException);
            Tools.Try(manager.Dispose, OnException);
        }

        //called from many threads
        private void OnDaemonLog(DaemonLog log)
        {
            outputs.Output(log.Timestamp, "{0} {1} {2} {3} {4}",
                log.Level, log.Uid, log.Name, log.Pid, log.Message);
        }

        //called from many threads
        private void OnException(Exception ex)
        {
            Tools.Try(() => Tools.Dump(ex));
            outputs.Output(ex.ToString());
        }
    }
}