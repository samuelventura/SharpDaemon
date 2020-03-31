using System;

namespace SharpDaemon.Server
{
    public class InteractiveFactory : ShellFactory
    {
        private readonly Manager manager;
        private readonly Action<Exception> handler;

        public class Args
        {
            public Manager Manager { get; set; }
            public Action<Exception> ExceptionHandler { get; set; }
        }

        public InteractiveFactory(Args args)
        {
            manager = args.Manager;
            handler = args.ExceptionHandler;
        }

        public Shell Create()
        {
            return new Interactive(new Interactive.Args
            {
                Manager = manager,
                ExceptionHandler = handler,
            });
        }

        public void Dispose()
        {
        }
    }

    public class Interactive : Shell
    {
        private readonly Manager manager;
        private readonly Action<Exception> handler;

        public class Args
        {
            public Manager Manager { get; set; }
            public Action<Exception> ExceptionHandler { get; set; }
        }

        public Interactive(Args args)
        {
            manager = args.Manager;
            handler = args.ExceptionHandler;
        }

        public void Dispose()
        {
        }

        public void OnLine(string line, Action<string> output)
        {
        }
    }

}