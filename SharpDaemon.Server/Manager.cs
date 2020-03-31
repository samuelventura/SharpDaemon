using System;
using System.Collections.Generic;

namespace SharpDaemon.Server
{
    public class Manager : IDisposable, IScriptable
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
            using (var disposer = new Disposer(handler))
            {
                runner = new Runner(new Runner.Args { ExceptionHandler = handler });
                disposer.Push(runner);
                disposer.Clear();
            }
        }

        public void Dispose()
        {
            Tools.Try(runner.Dispose, handler);
            Tools.Try(controller.Dispose, handler);
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
                    var list = new List<string>();
                    list.Add("daemon");
                    list.Add("install");
                    list.Add(dto.Id);
                    list.Add(dto.Path);
                    list.Add(dto.Args);
                    controller.Execute(list.ToArray(), output);
                }
                named.Output("{0} daemon(s) loaded", dtos.Count);
            }, named.OnException);
        }

        public void Execute(string[] tokens, Output output)
        {
            if (tokens[0] == "daemon")
            {
                var named = new NamedOutput("MANAGER", output);
                if (tokens.Length == 2 && tokens[1] == "list")
                {
                    runner.Run(() =>
                    {
                        named.Output("Id|Created|Path|Args");
                        var dtos = Database.List(dbpath);
                        foreach (var dto in dtos)
                        {
                            var created = dto.Created.ToString("yyyy-MM-dd HH:mm:ss.fff");
                            named.Output("{0}|{1}|{2}|{3}", dto.Id, created, dto.Path, dto.Args);
                        }
                        named.Output("{0} daemon(s)", dtos.Count);
                    }, named.OnException);
                }
                if (tokens.Length == 5 && tokens[1] == "install")
                {
                    runner.Run(() =>
                    {
                        var dto = new DaemonDto
                        {
                            Id = tokens[2],
                            Path = tokens[3],
                            Args = tokens[4],
                        };
                        Database.Save(dbpath, dto);
                        controller.Execute(tokens, output);
                    }, named.OnException);
                }
                if (tokens.Length == 3 && tokens[1] == "uninstall")
                {
                    runner.Run(() =>
                    {
                        var id = tokens[2];
                        Database.Remove(dbpath, id);
                        controller.Execute(tokens, output);
                    }, named.OnException);
                }
            }
        }
    }
}