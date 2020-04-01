using System;

namespace SharpDaemon.Server
{
    public partial class Manager : Disposable
    {
        private readonly string dbpath;
        private readonly Runner runner;
        private readonly string downloads;
        private readonly Controller controller;
        private readonly Action<Exception> handler;

        public class Args
        {
            public string Downloads { get; set; }
            public string DatabasePath { get; set; }
            public Controller Controller { get; set; }
            public Action<Exception> ExceptionHandler { get; set; }
        }

        public Manager(Args args)
        {
            downloads = args.Downloads;
            dbpath = args.DatabasePath;
            controller = args.Controller;
            handler = args.ExceptionHandler;
            runner = new Runner(new Runner.Args
            {
                ExceptionHandler = handler,
                ThreadName = "Manager",
            });
        }

        protected override void Dispose(bool disposed)
        {
            Tools.Try(runner.Dispose);
            Tools.Try(controller.Dispose);
        }

        public void Start(Output output)
        {
            var named = new NamedOutput("MANAGER", output);

            Execute(named, () => ExecuteStart(output, named));
        }
    }
}