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
        private readonly NamedOutput named;

        public class Args
        {
            public int RestartDelay { get; set; }
            public string IpAddress { get; set; }
            public int TcpPort { get; set; }
            public string DbPath { get; set; }
            public string Downloads { get; set; }
            public Output Output { get; set; }
        }

        public Instance(Args args)
        {
            named = new NamedOutput("INSTANCE", args.Output);
            using (var disposer = new Disposer())
            {
                named.WriteLine("DbPath {0}", args.DbPath);
                named.WriteLine("Downloads {0}", args.Downloads);
                named.WriteLine("RestartDelay {0}ms", args.RestartDelay);
                named.WriteLine("IpAddress {0}", args.IpAddress);
                named.WriteLine("TcpPort {0}", args.TcpPort);
                factory = new ShellFactory();
                factory.Add(new SystemScriptable());
                factory.Add(new DownloadScriptable(args.Downloads));
                manager = new Manager(new Manager.Args
                {
                    RestartDelay = args.RestartDelay,
                    Root = args.Downloads,
                    Database = args.DbPath,
                    Output = named.Output,
                });
                disposer.Push(manager);
                factory.Add(manager);
                listener = new Listener(new Listener.Args
                {
                    IpAddress = args.IpAddress,
                    ShellFactory = factory,
                    TcpPort = args.TcpPort,
                    Output = named.Output,
                });
                disposer.Push(listener);
                factory.Add(listener);
                endpoint = listener.EndPoint;

                disposer.Push(Dispose); //ensure cleanup order
                listener.Start();
                disposer.Clear();
            }
        }

        public IPEndPoint EndPoint { get { return endpoint; } }

        public Shell CreateShell() { return factory.Create(); }

        protected override void Dispose(bool disposed)
        {
            listener.Dispose();
            manager.Dispose();
        }
    }
}