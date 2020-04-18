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
                    shell.Execute(@"run cmd.exe");
                    shell.WaitFor(400, @"<o Process \d+ has started");
                    shell.WaitFor(400, @"<o Microsoft Windows");
                    shell.Execute(@"cd c:\Users");
                    shell.Execute(@"dir");
                    shell.WaitFor(400, @"\d+ File");
                    shell.WaitFor(400, @"\d+ Dir");
                    shell.Execute(@"exit"); //cmd.exe
                    shell.WaitFor(400, @"<o Process \d+ has exited"); //prevent swallowing of exit!
                    shell.Execute(@"exit!"); //shell
                    shell.WaitFor(400, Environ.NewLines);
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
                        shell.WaitFor(400, $@"Register Client {lastEP} connected");

                        client.Execute(@"run cmd.exe");
                        client.WaitFor(400, @"<c Process \d+ has started");
                        client.WaitFor(400, @"<c Microsoft Windows");
                        client.Execute(@"cd c:\Users");
                        client.Execute(@"dir");
                        client.WaitFor(400, @"\d+ File");
                        client.WaitFor(400, @"\d+ Dir");
                        client.Execute(@"exit"); //cmd.exe
                        client.WaitFor(400, @"<c Process \d+ has exited"); //prevent swallowing of exit!
                    });
                    shell.WaitFor(400, $@"Register Client {lastEP} disconnected");
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
                    var count = 20;
                    for (var i = 0; i < count; i++)
                    {
                        var task = Task.Run(() =>
                        {
                            //macos get connection refused because shell not ready
                            Thread.Sleep(400);

                            TestTools.Client(config, (client, endpoint) =>
                            {
                                client.Execute(@"run cmd.exe");
                                client.WaitFor(400, @"<c Process \d+ has started");
                                client.WaitFor(400, @"<c Microsoft Windows");
                                client.Execute(@"cd c:\Users");
                                client.Execute(@"dir");
                                client.WaitFor(400, @"\d+ File");
                                client.WaitFor(400, @"\d+ Dir");
                                client.Execute(@"exit"); //cmd.exe
                                client.WaitFor(400, @"<c Process \d+ has exited"); //prevent swallowing of exit!
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
