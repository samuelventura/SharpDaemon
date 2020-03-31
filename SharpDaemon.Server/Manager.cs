using System;

namespace SharpDaemon.Server
{
    public class Manager : IDisposable, ShellCommand
    {
        private readonly string dbpath;
        private readonly Runner runner;
        private readonly Controller controller;
        private readonly Action<Exception> handler;

        public class Args
        {
            public string DatabasePath { get; set; }
            public Controller Controller { get; set; }
            public Action<Exception> ExceptionHandler { get; set; }
        }

        public Manager(Args args)
        {
            dbpath = args.DatabasePath;
            controller = args.Controller;
            handler = args.ExceptionHandler;
            using (var disposer = new Disposer(handler))
            {
                runner = new Runner(new Runner.Args { ExceptionHandler = handler });
                disposer.Push(runner);
                runner.Run(() =>
                {
                    foreach (var dto in Database.List(dbpath)) controller.Start(dto);
                });
                disposer.Clear();
            }
        }

        public void Install(string path, string args, Callback callback = null)
        {
            runner.Run(() =>
            {
                var dto = new DaemonDto
                {
                    Path = path,
                    Args = args,
                };
                Database.Save(dbpath, dto);
                controller.Start(dto, callback);
            }, callback?.Handler);
        }

        public void Uninstall(string id, Callback callback = null)
        {
            runner.Run(() =>
            {
                Database.Remove(dbpath, id);
                controller.Stop(id, callback);
            }, callback?.Handler);
        }

        public void Dispose()
        {
            Tools.Try(runner.Dispose, handler);
            Tools.Try(controller.Dispose, handler);
        }

        public void OnLine(string line, Output output)
        {
            if (line.Trim() == "lsd")
            {
                runner.Run(() =>
                {
                    var dtos = Database.List(dbpath);
                    foreach (var dto in dtos)
                    {
                        var created = dto.Created.ToString("yyyy-MM-dd HH:mm:ss.fff");
                        output.Output("{0} {1} {2} {3}", dto.Id, created, dto.Path, dto.Args);
                    }
                    output.Output("{0} daemon(s)", dtos.Count);
                });
            }
        }
    }
}