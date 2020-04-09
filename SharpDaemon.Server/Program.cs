using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Diagnostics;

namespace SharpDaemon.Server
{
    class Program
    {
        class Config
        {
            public string Id { get; set; }
            public bool Daemon { get; set; }
            public string Root { get; set; }
            public string IP { get; set; }
            public int Port { get; set; }
            public int Delay { get; set; } //process restart delay (ms)
            public int Timeout { get; set; } //dispose timeout (ms)
        }

        static void Main(string[] args)
        {

            ExceptionTools.SetupDefaultHandler();
            Thread.CurrentThread.Name = "Main";

            var config = new Config();

            config.IP = "0.0.0.0";
            config.Port = 22333;
            config.Delay = 5000;
            config.Timeout = 5000;
            config.Root = ExecutableTools.Relative("Workspace");

            Stdio.WriteLine("Args {0} {1}", args.Length, string.Join(" ", args));

            for (var i = 0; i < args.Length; i++)
            {
                Stdio.WriteLine("Arg {0} {1}", i, args[i]);

                ConfigTools.SetProperty(config, args[i]);
            }

            AssertTools.NotEmpty(config.Root, "Missing root path");
            AssertTools.NotEmpty(config.IP, "Missing IP");
            AssertTools.Ip(config.IP, "Invalid IP");

            var writers = new WriteLineCollection();
            var timed = new TimedWriter(writers);
            if (!config.Daemon)
            {
                writers.Add(new ConsoleWriteLine());
                var pid = Process.GetCurrentProcess().Id;
                Output.TRACE = new NamedOutput(timed, string.Format("TRACE_{0}", pid));
            }

            var named = new NamedOutput(timed, "STDIN");

            named.WriteLine("Workspace {0}", config.Root);

            timeout = config.Timeout;
            var dbpath = Path.Combine(config.Root, "SharpDaemon.LiteDb");
            var logpath = Path.Combine(config.Root, "SharpDaemon.Log.txt");
            var eppath = Path.Combine(config.Root, "SharpDaemon.Endpoint.txt");
            var downloads = Path.Combine(config.Root, "Downloads");

            Directory.CreateDirectory(downloads); //creates root as well

            //acts like mutex for the workspace
            var log = new StreamWriter(logpath, true);
            writers.Add(new TextWriterWriteLine(log));
            var output = new Output(timed);

            var instance = new Instance(new Instance.Args
            {
                DbPath = dbpath,
                RestartDelay = config.Delay,
                Downloads = downloads,
                Output = output,
                EndPoint = new IPEndPoint(IPAddress.Parse(config.IP), config.Port),
            });
            using (var disposer = new Disposer())
            {
                disposer.Push(Disposed);
                disposer.Push(log);
                disposer.Push(instance);
                disposer.Push(Disposing);

                if (config.Daemon)
                {
                    Stdio.WriteLine("Stdin loop...");
                    var line = Stdio.ReadLine();
                    while (line != null)
                    {
                        named.WriteLine("> {0}", line);
                        if ("exit!" == line) break;
                        line = Stdio.ReadLine();
                    }
                    Stdio.WriteLine("Stdin closed");
                }
                else
                {
                    named.WriteLine("Stdin loop...");
                    var shell = instance.CreateShell();
                    var stream = new ShellStream(new NamedOutput(output, "SHELL <"), new ConsoleReadLine());
                    //disposer.Push(stream); //stdin.readline wont return even after close/dispose call
                    var line = stream.ReadLine();
                    while (line != null)
                    {
                        named.WriteLine("> {0}", line);
                        if ("exit!" == line) break;
                        Shell.ParseAndExecute(shell, stream, line);
                        line = stream.ReadLine();
                    }
                    named.WriteLine("Stdin closed");
                }
            }
        }

        static volatile bool disposed;
        static volatile int timeout;

        static void Disposed()
        {
            disposed = true;
        }

        static void Disposing()
        {
            var thread = new Thread(() =>
            {
                var dl = DateTime.Now.AddMilliseconds(timeout);
                while (true)
                {
                    if (DateTime.Now > dl) throw new Exception("Timeout waiting dispose");
                    if (disposed) return;
                    Thread.Sleep(1);
                }
            });
            thread.IsBackground = true;
            thread.Name = "Dispose monitor";
            thread.Start();
        }
    }
}
