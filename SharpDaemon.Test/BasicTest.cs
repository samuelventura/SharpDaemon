using System;
using System.IO;
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
                    shell.WaitFor(400, @"<o Listening on 127.0.0.1:12333");
                    shell.Execute("exit!");
                    shell.WaitFor(400, Environ.NewLines);
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
                    shell.WaitFor(400, @"<o Listening on 127.0.0.1:12333");
                    shell.Execute("exit!");
                    shell.WaitFor(400, Environ.NewLines);
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
                    shell.WaitFor(400, @"<o \d+ total counts");
                    shell.WaitFor(400, @"<o \d+ total undisposed");
                });
            }
        }

        [Test]
        public void ShellSystemPasswordTest()
        {
            using (var config = new Config())
            {
                TestTools.Shell(config, (shell) =>
                {
                    var passfile = ExecutableTools.Relative("Password.txt");
                    var password = File.ReadAllText(passfile).Trim();
                    shell.Execute($@"system password {password}");
                    shell.WaitFor(400, @"<o Password changed");
                });
            }
        }
    }
}
