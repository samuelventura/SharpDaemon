using System;
using System.Threading;
using NUnit.Framework;

namespace SharpDaemon.Test
{
    public class DaemonServerTest
    {
        [Test]
        public void DaemonStartTest()
        {
            TestTools.Run(true, (env, shell) =>
            {
                shell.WaitFor(400, @"Stdin loop...");
                shell.Execute("exit");
                shell.WaitFor(400, @"Stdin closed");
            });
        }

        [Test]
        public void SystemCountsTest()
        {
            TestTools.Run(false, (env, shell) =>
            {
                shell.WaitFor(400, @"Stdin loop...");

                shell.Execute("system", "counts");
                shell.WaitFor(400, @"\d+ total counts");
                shell.WaitFor(400, @"\d+ total undisposed");

                shell.Execute("exit");
                shell.WaitFor(400, @"Stdin closed");
            });
        }

        //[Test]
        public void BasicTest()
        {
            TestTools.Run(false, (env, shell) =>
            {
                shell.Execute("download", "zip", $@"http://{env.WebEP}/Daemon.StaticWebServer.zip");
                shell.WaitFor(400, @"Downloaded to Daemon.StaticWebServer.zip");
                shell.Execute("daemon", "install", "web", @"Daemon.StaticWebServer.zip\Daemon.StaticWebServer.exe", $"Endpoint=127.0.0.1:12335", $"Root={env.WebRoot}");
                shell.WaitFor(400, @"MANAGER Daemon web started Daemon.StaticWebServer\|\d+");
                shell.WaitFor(400, $@"web_\d+ < Serving at http://127.0.0.1:12335");

                Thread.Sleep(100);
                //Environment.Exit(0); brute force
            });
        }

        //[Test]
        public void CmdTest()
        {
            TestTools.Run(false, (env, shell) =>
            {
                shell.Execute("run", "cmd");
                shell.WaitFor(400, @"Downloaded to Daemon.StaticWebServer.zip");

                Thread.Sleep(100);
                //Environment.Exit(0); brute force
            });
        }

    }
}
