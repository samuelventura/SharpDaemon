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
            named = new NamedOutput("LISTENER", args.Output);
            using (var disposer = new Disposer())
            {
                named.WriteLine("DbPath {0}", args.DbPath);
                named.WriteLine("Downloads {0}", args.Downloads);
                named.WriteLine("RestartDelay {0}s", args.RestartDelay);
                named.WriteLine("IpAddress {0}", args.IpAddress);
                named.WriteLine("TcpPort {0}", args.TcpPort);
                factory = new ShellFactory();
                factory.Add(new SystemScriptable());
                manager = new Manager(new Manager.Args
                {
                    RestartDelay = args.RestartDelay,
                    Downloads = args.Downloads,
                    Database = args.DbPath,
                    Output = named.Output,
                });
                disposer.Push(manager);
                listener = new Listener(new Listener.Args
                {
                    IpAddress = args.IpAddress,
                    ShellFactory = factory,
                    TcpPort = args.TcpPort,
                    Output = named.Output,
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
            listener.Dispose();
            manager.Dispose();
        }
    }
}