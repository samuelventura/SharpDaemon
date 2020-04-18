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

        public class Args
        {
            public int RestartDelay { get; set; }
            public IPEndPoint EndPoint { get; set; }
            public string DbPath { get; set; }
            public string Downloads { get; set; }
        }

        public Instance(Args args)
        {
            using (var disposer = new Disposer())
            {
                Logger.Trace("DbPath {0}", args.DbPath);
                Logger.Trace("Downloads {0}", args.Downloads);
                Logger.Trace("RestartDelay {0}ms", args.RestartDelay);
                Logger.Trace("EndPoint {0}", args.EndPoint);
                factory = new ShellFactory();
                factory.Add(new SystemScriptable());
                factory.Add(new RunnerScriptable(args.Downloads));
                factory.Add(new DownloadScriptable(args.Downloads));
                manager = new Manager(new Manager.Args
                {
                    RestartDelay = args.RestartDelay,
                    Root = args.Downloads,
                    Database = args.DbPath,
                });
                disposer.Push(manager);
                factory.Add(manager);
                listener = new Listener(new Listener.Args
                {
                    ShellFactory = factory,
                    EndPoint = args.EndPoint,
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
            //first kill all processes
            manager.Dispose();
            //now stop listening
            listener.Dispose();
        }
    }
}