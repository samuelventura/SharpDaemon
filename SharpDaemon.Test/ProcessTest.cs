using System;
using NUnit.Framework;

namespace SharpDaemon.Test
{
    public class ProcessTest
    {
        [Test]
        public void DaemonPingAndExitTest()
        {
            using (var simple = new Simple())
            {
                var path = ExecutableTools.Relative("Daemon.Test.exe");
                var args = "Daemon=false";
                TestTools.Shell(path, args, (shell) =>
                {
                    shell.WaitFor(400, @"<o Ready");
                    shell.Execute("ping");
                    shell.WaitFor(400, @"<o pong");
                    shell.Execute("exit!");
                    shell.WaitFor(800, Environ.NewLines);
                });
            }
        }

        [Test]
        public void DaemonPingAndThrowTest()
        {
            using (var simple = new Simple())
            {
                var path = ExecutableTools.Relative("Daemon.Test.exe");
                var args = "Daemon=false";
                TestTools.Shell(path, args, (shell) =>
                {
                    shell.WaitFor(400, @"<o Ready");
                    shell.Execute("ping");
                    shell.WaitFor(400, @"<o pong");
                    shell.Execute("throw EXCEPTION");
                    shell.WaitFor(800, Environ.NewLines);
                });
            }
        }
    }
}
