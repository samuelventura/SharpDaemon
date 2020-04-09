using System;
using NUnit.Framework;

namespace SharpDaemon.Test
{
    public class BasicTest
    {
        [Test]
        public void DaemonLoopExitTest()
        {
            using (var config = new Config(true))
            {
                TestTools.Shell(config, (shell) =>
                {
                    shell.WaitFor(400, @"Stdin loop...");
                    shell.Execute("exit!");
                    shell.WaitFor(400, @"Stdin closed");
                });
            }
        }

        [Test]
        public void ShellLoopExitTest()
        {
            using (var config = new Config())
            {
                TestTools.Shell(config, (shell) =>
                {
                    shell.WaitFor(400, @"Stdin loop...");
                    shell.Execute(@"exit!");
                    shell.WaitFor(400, @"Stdin closed");
                });
            }
        }

        [Test]
        public void ShellSystemCountsTest()
        {
            using (var config = new Config())
            {
                TestTools.Shell(config, (shell) =>
                {
                    shell.Execute(@"system counts");
                    shell.WaitFor(400, @"\d+ total counts");
                    shell.WaitFor(400, @"\d+ total undisposed");
                });
            }
        }
    }
}
