using System;
using System.IO;
using System.Net;
using SharpDaemon;

namespace Daemon.StaticWebServer
{
    class Program
    {
        class Config
        {
            public string Id { get; set; }
            public string Root { get; set; }
            public string EndPoint { get; set; }
            public bool Trace { get; set; }
        }

        static void Main(string[] args)
        {
            ExceptionTools.SetupDefaultHandler();

            var config = new Config();

            Stdio.WriteLine("Args {0} {1}", args.Length, string.Join(" ", args));

            for (var i = 0; i < args.Length; i++)
            {
                Stdio.WriteLine("Arg {0} {1}", i, args[i]);

                ConfigTools.SetProperty(config, args[i]);
            }

            AssertTools.NotEmpty(config.EndPoint, "Missing EndPoint");
            AssertTools.NotEmpty(config.Root, "Missing Root");
            if (config.Trace) Output.TRACE = new ConsoleWriteLine();

            var uri = string.Format("http://{0}/", config.EndPoint);
            var http = new HttpListener();
            http.Prefixes.Add(uri);
            http.Start();
            var accepter = new Runner(new Runner.Args { ExceptionHandler = Output.Trace });
            var handler = new Runner(new Runner.Args { ExceptionHandler = Output.Trace });
            accepter.Run(() =>
            {
                while (http.IsListening)
                {
                    var ctx = http.GetContext();
                    handler.Run(() =>
                    {
                        var request = ctx.Request;
                        var response = ctx.Response;
                        var pass = true;
                        var file = ctx.Request.RawUrl.Substring(1); //remove leading /
                        if (!PathTools.IsChildPath(config.Root, file)) pass = false;
                        var path = PathTools.Combine(config.Root, file);
                        Output.Trace("File {0} {1}", file, path);
                        if (!File.Exists(path)) pass = false;
                        if (ctx.Request.HttpMethod != "GET") pass = false;
                        if (pass)
                        {
                            var fi = new FileInfo(path);
                            var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                            ctx.Response.StatusCode = (int)HttpStatusCode.OK;
                            ctx.Response.ContentLength64 = fi.Length;
                            ctx.Response.ContentType = "application/octet-stream";
                            var data = new byte[1024];
                            var count = fs.Read(data, 0, data.Length);
                            while (count > 0)
                            {
                                ctx.Response.OutputStream.Write(data, 0, count);
                                count = fs.Read(data, 0, data.Length);
                            }
                        }
                        else
                        {
                            ctx.Response.StatusCode = (int)HttpStatusCode.NotFound;
                            ctx.Response.ContentLength64 = 0;
                        }
                        ctx.Response.Close();
                    });
                }
            });

            Stdio.WriteLine("Serving at {0}", uri);

            using (var disposer = new Disposer())
            {
                disposer.Push(handler);
                disposer.Push(accepter);
                disposer.Push(http.Stop);

                var line = Stdio.ReadLine();
                while (line != null) line = Stdio.ReadLine();
            }

            Stdio.WriteLine("Stdin closed");

            Environment.Exit(0);
        }
    }
}