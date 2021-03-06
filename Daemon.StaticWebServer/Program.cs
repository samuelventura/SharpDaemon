﻿using System;
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
            public bool Daemon { get; set; }
            public string Root { get; set; }
            public string EndPoint { get; set; }
        }

        static void Main(string[] args)
        {
            ProgramTools.Setup();

            var config = new Config();

            //args will always log to stderr because trace flag not loaded yet
            ExecutableTools.LogArgs(new StderrWriteLine(), args, (arg) =>
            {
                ConfigTools.SetProperty(config, arg);
            });

            AssertTools.NotEmpty(config.EndPoint, "Missing EndPoint");
            AssertTools.NotEmpty(config.Root, "Missing Root");

            if (!config.Daemon) Logger.TRACE = new StderrWriteLine();

            var uri = string.Format("http://{0}/", config.EndPoint);
            var http = new HttpListener();
            http.Prefixes.Add(uri);
            http.Start();
            var accepter = new Runner(new Runner.Args { ThreadName = "Accepter" });
            var handler = new Runner(new Runner.Args { ThreadName = "Handler" });
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
                        Logger.Trace("File {0} {1}", file, path);
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

            Stdio.SetStatus("Listeninig on {0}", uri);

            using (var disposer = new Disposer())
            {
                disposer.Push(handler);
                disposer.Push(accepter);
                disposer.Push(http.Stop);

                var line = Stdio.ReadLine();
                while (line != null) line = Stdio.ReadLine();
            }

            Logger.Trace("Stdin closed");

            Environment.Exit(0);
        }
    }
}