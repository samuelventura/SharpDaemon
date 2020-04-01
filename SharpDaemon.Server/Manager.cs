using System;

namespace SharpDaemon.Server
{
    public class Manager : Disposable, IScriptable
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
            runner.Run(() =>
            {
                named.Output("Loading daemons...");
                var dtos = Database.List(dbpath);
                foreach (var dto in dtos)
                {
                    var created = dto.Created.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    named.Output("Loading daemon {0}|{1}|{2}|{3}", dto.Id, created, dto.Path, dto.Args);
                    controller.Execute(output, "daemon", "install", dto.Id, dto.Path, dto.Args);
                }
                named.Output("{0} daemon(s) loaded", dtos.Count);
            }, named.OnException);
        }

        public void Execute(Output output, params string[] tokens)
        {
            if (tokens[0] == "daemon")
            {
                var named = new NamedOutput("MANAGER", output);
                if (tokens.Length == 2 && tokens[1] == "status")
                {
                    controller.Execute(output, tokens);
                }
                if (tokens.Length == 2 && tokens[1] == "list")
                {
                    runner.Run(() =>
                    {
                        named.Output("Id|Created|Path|Args");
                        var dtos = Database.List(dbpath);
                        foreach (var dto in dtos)
                        {
                            named.Output("{0}|{1}|{2}|{3}", dto.Id, Tools.Format(dto.Created), dto.Path, dto.Args);
                        }
                        named.Output("{0} daemon(s)", dtos.Count);
                    }, named.OnException);
                }
                if ((tokens.Length == 4 || tokens.Length == 5) && tokens[1] == "install")
                {
                    runner.Run(() =>
                    {
                        var dto = new DaemonDto
                        {
                            Id = tokens[2],
                            Path = tokens[3],
                            Created = DateTime.Now,
                            Args = tokens.Length > 4 ? tokens[4] : string.Empty,
                        };
                        named.Output("Installing... {0}|{1}|{2}|{3}", dto.Id, Tools.Format(dto.Created), dto.Path, dto.Args);
                        Database.Save(dbpath, dto);
                        controller.Execute(output, tokens);
                    }, named.OnException);
                }
                if (tokens.Length == 3 && tokens[1] == "uninstall")
                {
                    runner.Run(() =>
                    {
                        var id = tokens[2];
                        named.Output("Uninstalling... {0}", id);
                        Database.Remove(dbpath, id);
                        controller.Execute(output, tokens);
                    }, named.OnException);
                }
            }
        }
    }
}