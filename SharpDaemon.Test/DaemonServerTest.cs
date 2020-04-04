using System;
using System.IO;
using System.Threading;
using System.IO.Compression;
using System.Text.RegularExpressions;
using NUnit.Framework;
using SharpDaemon.Server;

namespace SharpDaemon.Test
{
    public class DaemonServerTest
    {
        [Test]
        public void BasicTest()
        {
            var testo = new TestOutput();
            var outputs = new Outputs();
            outputs.Add(testo);
            outputs.Add(new ConsoleOutput());
            Disposable.Debug = outputs;
            var cargs = new Launcher.CliArgs
            {
                Delay = 0, //gets 100ms minimum
                Port = 0,
                Ip = "127.0.0.1",
                Ws = Tools.Relative("WS_{0}", Tools.Compact(DateTime.Now)),
            };
            var webroot = Tools.Relative(cargs.Ws, "WebRoot");
            Directory.CreateDirectory(webroot);
            var zippath = Tools.Combine(webroot, "Daemon.StaticWebServer.zip");
            Tools.ZipFromFiles(zippath, Tools.ExecutableDirectory()
                , "Daemon.StaticWebServer.exe"
                , "SharpDaemon.dll"
                , "Nancy.Hosting.Self.dll"
                , "Nancy.dll"
                , "LaunchStaticWebServer.bat"
            );
            ZipFile.ExtractToDirectory(zippath, Tools.Combine(cargs.Ws, "Downloads/web"));
            using (var disposer = new Disposer())
            {
                var io = new StreamIO(outputs, new ConsoleTextReader());
                disposer.Push(io);
                var instance = Launcher.Launch(outputs, cargs);
                disposer.Push(instance);
                //0.0.0.0 throws System.Net.HttpListenerException The request is not supported
                //localhost Nancy.Hosting.Self.AutomaticUrlReservationCreationFailureException The Nancy self host was unable to start, as no namespace reservation existed for the provided url(s).
                //192.168.1.69 Nancy.Hosting.Self.AutomaticUrlReservationCreationFailureException The Nancy self host was unable to start, as no namespace reservation existed for the provided url(s).
                var webep = "127.0.0.1:12334";
                var shell = instance.CreateShell();
                //Beware | is regex reserved `or`
                shell.Execute(io, "daemon", "install", "web", "web\\Daemon.StaticWebServer.exe", "Test=true", $"Endpoint={webep}", string.Format("Root={0}", webroot));
                testo.WaitFor(400, @"MANAGER Daemon web started Daemon.StaticWebServer\|\d+");
                testo.WaitFor(400, $@"web_\d+ < Serving at http://{webep}");
                shell.Execute(io, "download", "zip", $@"http://{webep}/Daemon.StaticWebServer.zip");
                testo.WaitFor(400, @"Downloaded to Daemon.StaticWebServer.zip");

                Thread.Sleep(2000);
            }

            //Environment.Exit(0);
        }

        class TestOutput : Output
        {
            private readonly LockedQueue<string> queue = new LockedQueue<string>();

            public override void WriteLine(string format, params object[] args)
            {
                var line = Tools.Format(format, args);
                queue.Push(line);
            }

            public void WaitFor(int toms, string format, params object[] args)
            {
                var dl = DateTime.Now.AddMilliseconds(toms);
                var pattern = Tools.Format(format, args);
                while (true)
                {
                    var line = queue.Pop(1, null);
                    if (line != null) Stdio.WriteLine("POP {0}", line);
                    if (line != null && Regex.IsMatch(line, pattern)) break;
                    if (DateTime.Now > dl) throw Tools.Make("Timeout waiting for `{0}`", pattern);
                }
            }
        }
    }
}
