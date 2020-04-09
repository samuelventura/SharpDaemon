using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
                    shell.WaitFor(400, @"Process \d+ has started");
                    shell.WaitFor(400, @"Microsoft Corporation");
                    shell.Execute(@"dir");
                    shell.WaitFor(400, @"\d+ File");
                    shell.WaitFor(400, @"\d+ Dir");
                    shell.Execute(@"exit"); //cmd.exe
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
                    var lastEP = string.Empty;
                    TestTools.Client(config, (client, endpoint) =>
                    {
                        lastEP = endpoint;
                        shell.WaitFor(400, $@"LISTENER Client {lastEP} connected");
                        client.Execute(@"run cmd.exe");
                        client.WaitFor(400, @"Process \d+ has started");
                        client.WaitFor(400, @"Microsoft Corporation");
                        client.Execute(@"dir");
                        client.WaitFor(400, @"\d+ File");
                        client.WaitFor(400, @"\d+ Dir");
                        client.Execute(@"exit"); //cmd.exe
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
                    for (var i = 0; i < 20; i++)
                    {
                        var task = Task.Run(() =>
                        {
                            TestTools.Client(config, (client, endpoint) =>
                            {
                                client.Execute(@"run cmd.exe");
                                client.WaitFor(400, @"Process \d+ has started");
                                client.WaitFor(400, @"Microsoft Corporation");
                                client.Execute(@"dir");
                                client.WaitFor(400, @"\d+ File");
                                client.WaitFor(400, @"\d+ Dir");
                                client.Execute(@"exit"); //cmd.exe
                                client.WaitFor(400, @"Process \d+ has exited"); //prevent swallowing of exit!
                                //throw new Exception("Exception!");
                            });
                        });
                        tasks.Add(task);
                    }
                    //AggregateException showing array exceptions for each throwing task (confirmed)
                    AssertTools.True(Task.WaitAll(tasks.ToArray(), 2000), "Timeout waiting tasks");
                });
            }
        }
    }
}