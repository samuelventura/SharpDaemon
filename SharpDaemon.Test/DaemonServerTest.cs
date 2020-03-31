using System;
using System.Text;
using System.Threading;
using System.Text.RegularExpressions;
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
            var testo = new TestOutput();
            var outputs = new Outputs();
            outputs.Add(testo);
            outputs.Add(new StdOutput());
            var cargs = new Launcher.CliArgs
            {
                Delay = 1000,
                Port = 0,
                Ip = "127.0.0.1",
                Ws = Tools.Relative("WS_{0}", Tools.Compact(DateTime.Now)),
            };
            using (var instance = Launcher.Launch(outputs, cargs))
            {
                var shell = instance.CreateShell();
                shell.Execute(outputs, "daemon", "install", "sample", Tools.Relative("SharpDaemon.Test.Daemon.exe"), "echo hello");
                testo.WaitFor(400, "MANAGER Installing... sample");
                testo.WaitFor(400, "CONTROLLER Process starting... sample");
                testo.WaitFor(400, "CONTROLLER Process started SharpDaemon.Test.Daemon");
                testo.WaitFor(400, "DAEMON Debug sample SharpDaemon.Test.Daemon \\d+ Arg echo");
                testo.WaitFor(400, "DAEMON Debug sample SharpDaemon.Test.Daemon \\d+ Arg hello");
                testo.WaitFor(400, "DAEMON Info sample SharpDaemon.Test.Daemon \\d+ hello");
                testo.WaitFor(400, "CONTROLLER Daemon sample restarting in \\d+ms");
                Thread.Sleep(2000);
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
