using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using NUnit.Framework;

namespace SharpDaemon.Test
{
    public class RunCmdTest
    {
        [Test]
        public void ShellRunCmdTest()
        {
            using (var config = new Config())
            {
                TestTools.Shell(config, (shell) =>
                {
                    if (Environ.IsWindows())
                    {
                        shell.Execute(@"run cmd.exe");
                        shell.WaitFor(400, @"Process \d+ has started");
                        shell.WaitFor(400, @"Microsoft Corporation");
                        shell.Execute(@"cd c:\Users");
                        shell.Execute(@"dir");
                        shell.WaitFor(400, @"\d+ File");
                        shell.WaitFor(400, @"\d+ Dir");
                        shell.Execute(@"exit"); //cmd.exe
                    }
                    else
                    {
                        shell.Execute(@"run bash");
                        shell.WaitFor(400, @"Process \d+ has started");
                        shell.Execute(@"cd $HOME");
                        shell.Execute(@"ls -l");
                        shell.WaitFor(400, @"total \d+");
                        shell.Execute(@"exit"); //bash
                    }
                    shell.WaitFor(400, @"Process \d+ has exited"); //prevent swallowing of exit!
                    shell.Execute(@"exit!"); //shell
                    shell.WaitFor(400, @"Stdin closed");
                });
            }
        }

        [Test]
        public void ClientRunCmdTest()
        {
            using (var config = new Config())
            {
                TestTools.Shell(config, (shell) =>
                {
                    //macos get connection refused because shell not ready
                    Thread.Sleep(400);

                    var lastEP = string.Empty;
                    TestTools.Client(config, (client, endpoint) =>
                    {
                        lastEP = endpoint;
                        shell.WaitFor(400, $@"LISTENER Client {lastEP} connected");

                        if (Environ.IsWindows())
                        {
                            client.Execute(@"run cmd.exe");
                            client.WaitFor(400, @"Process \d+ has started");
                            client.WaitFor(400, @"Microsoft Corporation");
                            client.Execute(@"cd c:\Users");
                            client.Execute(@"dir");
                            client.WaitFor(400, @"\d+ File");
                            client.WaitFor(400, @"\d+ Dir");
                            client.Execute(@"exit"); //cmd.exe
                        }
                        else
                        {
                            client.Execute(@"run bash");
                            client.WaitFor(400, @"Process \d+ has started");
                            client.Execute(@"cd $HOME");
                            client.Execute(@"ls -l");
                            client.WaitFor(400, @"total \d+");
                            client.Execute(@"exit"); //bash
                        }

                        client.WaitFor(400, @"Process \d+ has exited"); //prevent swallowing of exit!
                    });
                    shell.WaitFor(400, $@"LISTENER Client {lastEP} disconnected");
                });
            }
        }

        [Test]
        public void ClientRunNCmdTest()
        {
            using (var config = new Config())
            {
                TestTools.Shell(config, (shell) =>
                {
                    var tasks = new List<Task>();
                    //netcore linux in WSL maxes at 6 here
                    //netcoce macosL maxes at 7 here
                    var count = Environ.IsWindows() ? 20 : 6;
                    for (var i = 0; i < count; i++)
                    {
                        var task = Task.Run(() =>
                        {
                            //macos get connection refused because shell not ready
                            Thread.Sleep(400);

                            TestTools.Client(config, (client, endpoint) =>
                            {
                                if (Environ.IsWindows())
                                {
                                    client.Execute(@"run cmd.exe");
                                    client.WaitFor(400, @"Process \d+ has started");
                                    client.WaitFor(400, @"Microsoft Corporation");
                                    client.Execute(@"cd c:\Users");
                                    client.Execute(@"dir");
                                    client.WaitFor(400, @"\d+ File");
                                    client.WaitFor(400, @"\d+ Dir");
                                    client.Execute(@"exit"); //cmd.exe
                                }
                                else
                                {
                                    client.Execute(@"run bash");
                                    client.WaitFor(400, @"Process \d+ has started");
                                    client.Execute(@"cd $HOME");
                                    client.Execute(@"ls -l");
                                    client.WaitFor(400, @"total \d+");
                                    client.Execute(@"exit"); //bash
                                }
                                client.WaitFor(400, @"Process \d+ has exited"); //prevent swallowing of exit!
                                //throw new Exception("Exception!");
                            });
                        });
                        tasks.Add(task);
                    }
                    //net462 works for 20 clients and 2000ms wait
                    //AggregateException showing array exceptions for each throwing task (confirmed)
                    AssertTools.True(Task.WaitAll(tasks.ToArray(), 2000), "Timeout waiting tasks");
                });
            }
        }
    }
}
