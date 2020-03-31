using System;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using NUnit.Framework;
using SharpDaemon.Server;
using SharpDaemon;

namespace SharpDaemon.Test
{
    public class DaemonServerTest
    {
        [Test]
        public void BasicTest()
        {
            var outputs = new Outputs();
            outputs.Add(new StdOutput());
            var cargs = new Launcher.CliArgs
            {
                Port = 0,
                Ip = "127.0.0.1",
                Ws = Tools.Relative("WS_{0}", Tools.Compact(DateTime.Now)),
            };
            using (var instance = Launcher.Launch(outputs, cargs))
            {
                var shell = instance.CreateShell();
                var cmd = c("daemon", "install", "sample",
                    Tools.Relative("SharpDaemon.Test.Daemon.exe"));
                shell.Execute(cmd, outputs);
            }
        }

        string[] c(params string[] tokens) => tokens;
    }
}
