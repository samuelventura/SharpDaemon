using System;
using System.Threading;
using System.Text.RegularExpressions;
using NUnit.Framework;
using SharpDaemon.Server;

namespace SharpDaemon.Test
{
    //Testing Environment Setup
    //In Shell $1
    // .\DebugDaemons.bat
    //In Shell #2
    // run $env:REPO=($pwd).path (set REPO=%CD%)
    // dotnet test SharpDaemon.Test
    class Env
    {
        public string Repo;
        public string WebEP;
        public string WebRoot;

        public Env()
        {
            Repo = Environment.GetEnvironmentVariable("REPO");
            WebRoot = $@"{Repo}\Output";
            WebEP = "127.0.0.1:12334";
        }
    }

    public class DaemonServerTest
    {
        [Test]
        public void BasicTest()
        {
            Run((env, shell, io, tio) =>
           {
               shell.Execute(io, "download", "zip", $@"http://{env.WebEP}/Daemon.StaticWebServer.zip");
               tio.WaitFor(400, @"Downloaded to Daemon.StaticWebServer.zip");
               shell.Execute(io, "daemon", "install", "web", @"Daemon.StaticWebServer.zip\Daemon.StaticWebServer.exe", $"Endpoint=127.0.0.1:12335", $"Root={env.WebRoot}");
               tio.WaitFor(400, @"MANAGER Daemon web started Daemon.StaticWebServer\|\d+");
               tio.WaitFor(400, $@"web_\d+ < Serving at http://127.0.0.1:12335");

               Thread.Sleep(100);
               //Environment.Exit(0); brute force
           });
        }

        void Run(Action<Env, IShell, Shell.IO, TestIO> test)
        {
            var env = new Env();

            var tio = new TestIO();
            var outputs = new Outputs();
            outputs.Add(tio);
            outputs.Add(new ConsoleOutput());
            Disposable.Debug = outputs;
            var cargs = new Launcher.CliArgs
            {
                Delay = 0, //gets 100ms minimum
                Port = 0,
                Ip = "127.0.0.1",
                Ws = Tools.Relative("WS_{0}", Tools.Compact(DateTime.Now)),
            };
            using (var disposer = new Disposer())
            {
                var io = new StreamIO(outputs, new ConsoleTextReader());
                disposer.Push(io);
                var instance = Launcher.Launch(outputs, cargs);
                disposer.Push(instance);
                var shell = instance.CreateShell();
                test(env, shell, io, tio);
            }
        }

        class TestIO : Output
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
                    //if (line != null) Stdio.WriteLine("POP {0}", line);
                    //Beware | is regex reserved `or`
                    if (line != null && Regex.IsMatch(line, pattern)) break;
                    if (DateTime.Now > dl) throw Tools.Make("Timeout waiting for `{0}`", pattern);
                }
            }
        }
    }
}
