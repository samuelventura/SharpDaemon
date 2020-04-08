using System;
using NUnit.Framework;

namespace SharpDaemon.Test
{
    public class DaemonServerTest
    {
        [Test]
        public void DaemonLoopExitTest()
        {
            using (var config = new Config())
            {
                config.Daemon = true;
                TestTools.Run(config, (shell) =>
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
                TestTools.Run(config, (shell) =>
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
                TestTools.Run(config, (shell) =>
                {
                    shell.Execute(@"system counts");
                    shell.WaitFor(400, @"\d+ total counts");
                    shell.WaitFor(400, @"\d+ total undisposed");
                });
            }
        }

        [Test]
        public void ShellRunCmdTest()
        {
            using (var config = new Config())
            {
                TestTools.Run(config, (shell) =>
                {
                    shell.Execute(@"run cmd.exe");
                    shell.WaitFor(400, @"Process \d+ has started");
                    shell.WaitFor(400, @"Microsoft Corporation");
                    shell.Execute(@"dir");
                    shell.WaitFor(400, @"\d+ File");
                    shell.WaitFor(400, @"\d+ Dir");
                    shell.Execute(@"exit"); //cmd.exe
                                            //prevent swallowing of exit!
                    shell.WaitFor(400, @"Process \d+ has exited");
                    shell.Execute(@"exit!"); //shell
                    shell.WaitFor(400, @"Stdin closed");
                });
            }
        }

        [Test]
        public void ShellDownloadZipTest()
        {
            using (var config = new Config())
            {
                TestTools.Web(config, () =>
                {
                    TestTools.Run(config, (shell) =>
                    {
                        shell.Execute($@"download zip http://{config.WebEP}/Daemon.StaticWebServer.zip");
                        shell.WaitFor(1200, @"Downloaded to Daemon.StaticWebServer.zip"); //first takes longer
                        shell.Execute(@"download rename Daemon.StaticWebServer.zip Daemon.StaticWebServer.zip_old");
                        shell.WaitFor(400, @"Directory Daemon.StaticWebServer.zip renamed to Daemon.StaticWebServer.zip_old");
                        shell.Execute($@"download zip http://{config.WebEP}/Daemon.StaticWebServer.zip");
                        shell.WaitFor(400, @"Downloaded to Daemon.StaticWebServer.zip");
                        shell.Execute($@"download zip http://{config.WebEP}/Daemon.StaticWebServer.zip");
                        shell.WaitFor(400, @"Download directory already exists Daemon.StaticWebServer.zip");
                        shell.Execute(@"download rename Daemon.StaticWebServer.zip Daemon.StaticWebServer.zip_old");
                        shell.WaitFor(400, @"Directory Daemon.StaticWebServer.zip_old already exist");
                        shell.Execute(@"download list");
                        shell.WaitFor(400, @"2 total directories");
                        shell.Execute(@"download delete Daemon.StaticWebServer.zip_old");
                        shell.WaitFor(400, @"Directory Daemon.StaticWebServer.zip_old deleted");
                        shell.Execute(@"download list");
                        shell.WaitFor(400, @"1 total directories");
                        shell.WaitFor(400, @"0 total files");
                        shell.Execute(@"download list Daemon.StaticWebServer.zip");
                        shell.WaitFor(400, @"0 total directories");
                        shell.WaitFor(400, @"4 total files");
                        shell.Execute(@"download delete NONEXISTING");
                        shell.WaitFor(400, @"Directory NONEXISTING not found");
                        //should not allow any navigation like . or ..
                        shell.Execute(@"download delete ./NONEXISTING");
                        shell.WaitFor(400, @"Directory ./NONEXISTING not found");
                        shell.Execute(@"download delete ../NONEXISTING");
                        shell.WaitFor(400, @"Directory ../NONEXISTING not found");
                        shell.Execute(@"download rename NONEXISTING NEWNAME");
                        shell.WaitFor(400, @"Directory NONEXISTING not found");
                        shell.Execute(@"download rename Daemon.StaticWebServer.zip ./NEWNAME");
                        shell.WaitFor(400, @"Invalid new name ./NEWNAME");
                    });
                });
            }
        }

        [Test]
        public void ShellDownloadAndRunTest()
        {
            using (var config = new Config())
            {
                TestTools.Web(config, () =>
                {
                    TestTools.Run(config, (shell) =>
                    {
                        shell.Execute($@"download zip http://{config.WebEP}/Daemon.StaticWebServer.zip");
                        shell.WaitFor(1200, @"Downloaded to Daemon.StaticWebServer.zip"); //first takes longer
                        shell.Execute(@"download rename Daemon.StaticWebServer.zip StaticWebServer");
                        shell.WaitFor(400, @"Directory Daemon.StaticWebServer.zip renamed to StaticWebServer");
                        shell.Execute($@"daemon install web StaticWebServer\Daemon.StaticWebServer.exe EndPoint=127.0.0.1:12335 Root={config.WebRoot}");
                        shell.WaitFor(400, @"MANAGER Daemon web started Daemon.StaticWebServer\|\d+");
                        shell.WaitFor(400, $@"web_\d+ < Serving at http://127.0.0.1:12335");
                        shell.Execute(@"download zip http://127.0.0.1:12335/Daemon.StaticWebServer.zip");
                        shell.WaitFor(400, @"Downloaded to Daemon.StaticWebServer.zip");
                    });
                });
            }
        }
    }
}
