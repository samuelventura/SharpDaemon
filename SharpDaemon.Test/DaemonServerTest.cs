using System;
using System.IO;
using System.Text;
using System.Threading;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using NUnit.Framework;
using SharpDaemon.Server;
using SharpDaemon;
using Nancy.Hosting.Self;
using Nancy.Conventions;
using Nancy;

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
            outputs.Add(new StdOutput());
            var cargs = new Launcher.CliArgs
            {
                Delay = 400,
                Port = 0,
                Ip = "127.0.0.1",
                Ws = Tools.Relative("WS_{0}", Tools.Compact(DateTime.Now)),
            };
            Directory.CreateDirectory(cargs.Ws);
            var zippath = Path.Combine(cargs.Ws, "sample.zip");
            using (var zip = ZipFile.Open(zippath, ZipArchiveMode.Create))
            {
                zip.CreateEntryFromFile(Tools.Relative("SharpDaemon.Test.Daemon.exe"), "SharpDaemon.Test.Daemon.exe");
                var main = zip.CreateEntry("Main.txt");
                using (var writer = new StreamWriter(main.Open()))
                {
                    writer.WriteLine("SharpDaemon.Test.Daemon.exe");
                }
                var args = zip.CreateEntry("Arguments.txt");
                using (var writer = new StreamWriter(args.Open()))
                {
                    writer.WriteLine("Mode=Echo Data=Hello Delay=200");
                }
            }
            var host = new NancyHost(new Bootstrapper() { Root = cargs.Ws }, new Uri($"http://127.0.0.1:9999"));
            host.Start();
            using (var instance = Launcher.Launch(outputs, cargs))
            {
                var shell = instance.CreateShell();
                //shell.Execute(outputs, "daemon", "install", @"http://127.0.0.1:9999/sample.zip");
                shell.Execute(outputs, "daemon", "install", "sample", @"..\..\SharpDaemon.Test.Daemon.exe", "Mode=Echo Data=Hello Delay=200");
                testo.WaitFor(400, "MANAGER Daemon sample installing...");
                testo.WaitFor(400, "CONTROLLER Daemon sample starting...");
                testo.WaitFor(400, "CONTROLLER Daemon sample started \\d+ SharpDaemon.Test.Daemon");
                testo.WaitFor(400, "CONTROLLER Daemon sample \\d+ < Hello");
                testo.WaitFor(1000, "CONTROLLER Daemon sample restarting after \\d+ms");
                shell.Execute(outputs, "daemon", "uninstall", "sample");
                testo.WaitFor(400, "MANAGER Daemon sample uninstalling...");
                testo.WaitFor(400, "CONTROLLER Daemon sample stopped");
                Thread.Sleep(200000);
            }
        }

        class Bootstrapper : DefaultNancyBootstrapper
        {
            public string Root;
            protected override void ConfigureConventions(NancyConventions conventions)
            {
                base.ConfigureConventions(conventions);
                conventions.StaticContentsConventions.Clear();
                conventions.StaticContentsConventions.Add
                (StaticContentConventionBuilder.AddDirectory("/", Root));
            }

        }
        class TestOutput : Output
        {
            private readonly LockedQueue<string> queue = new LockedQueue<string>();

            public void Output(string format, params object[] args)
            {
                var line = Tools.Format(format, args);
                queue.Push(line);
            }

            public void WaitFor(int toms, string pattern)
            {
                var dl = DateTime.Now.AddMilliseconds(toms);
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
