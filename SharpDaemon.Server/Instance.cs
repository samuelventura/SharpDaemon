using System;
using System.Net;

namespace SharpDaemon.Server
{
    public class Instance : Disposable
    {
        private readonly ShellFactory factory;
        private readonly IPEndPoint endpoint;
        private readonly Listener listener;
        private readonly Manager manager;
        private readonly Outputs outputs;

        public class Args
        {
            public int RestartDelay { get; set; }
            public string IpAddress { get; set; }
            public int TcpPort { get; set; }
            public string DbPath { get; set; }
            public string Downloads { get; set; }
            public Outputs Outputs { get; set; }
        }

        public Instance(Args args)
        {
            outputs = args.Outputs;
            var named = new NamedOutput("INSTANCE", outputs);
            using (var disposer = new Disposer())
            {
                named.Output("DbPath {0}", args.DbPath);
                named.Output("Downloads {0}", args.Downloads);
                named.Output("RestartDelay {0}", args.RestartDelay);
                named.Output("IpAddress {0}", args.IpAddress);
                named.Output("TcpPort {0}", args.TcpPort);
                factory = new ShellFactory();
                factory.Add(new SystemScriptable());
                var controller = new Controller(new Controller.Args
                {
                    ExceptionHandler = OnException,
                    DaemonLogger = OnDaemonLog,
                    RestartDelay = args.RestartDelay,
                    Output = outputs,
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
                    IpAddress = args.IpAddress,
                    ShellFactory = factory,
                    TcpPort = args.TcpPort,
                    Output = outputs,
                });
                disposer.Push(listener);
                endpoint = listener.EndPoint;
                factory.Add(listener);
                factory.Add(manager);
                //fill factory before start
                listener.Start();
                disposer.Clear();
            }
        }

        public IPEndPoint EndPoint { get { return endpoint; } }

        public Shell CreateShell() { return factory.Create(); }

        protected override void Dispose(bool disposed)
        {
            Tools.Try(listener.Dispose);
            Tools.Try(manager.Dispose);
        }

        //called from many threads
        private void OnDaemonLog(DaemonLog log)
        {
            outputs.Output("DAEMON {0} {1} {2} {3} {4}",
                log.Level, log.Id, log.Name, log.Pid, log.Message);
        }

        //called from many threads
        private void OnException(Exception ex)
        {
            Tools.Try(() => Tools.Dump(ex));
            outputs.Output("UNHANDLED {0}", ex.ToString());
        }
    }
}