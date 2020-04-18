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
            ProgramTools.Setup();

            var config = new Config();

            config.IP = "0.0.0.0";
            config.Port = 22333;
            config.Delay = 5000;
            config.Timeout = 5000;
            config.Root = ExecutableTools.Relative("Root");

            //args will always log to stderr because daemon flag not loaded yet
            ExecutableTools.LogArgs(new StderrWriteLine(), args, (arg) =>
            {
                ConfigTools.SetProperty(config, arg);
            });

            AssertTools.NotEmpty(config.Root, "Missing Root path");
            AssertTools.NotEmpty(config.IP, "Missing IP");
            AssertTools.Ip(config.IP, "Invalid IP");

            var pid = Process.GetCurrentProcess().Id;
            var writers = new WriteLineCollection();

            if (!config.Daemon)
            {
                writers.Add(new StderrWriteLine());
                Logger.TRACE = new TimedWriter(writers);
            }

            Logger.Trace("Root {0}", config.Root);

            var dbpath = Path.Combine(config.Root, "SharpDaemon.LiteDb");
            var logpath = Path.Combine(config.Root, "SharpDaemon.Log.txt");
            var eppath = Path.Combine(config.Root, "SharpDaemon.Endpoint.txt");
            var downloads = Path.Combine(config.Root, "Downloads");

            Directory.CreateDirectory(downloads); //creates root as well

            //acts like mutex for the workspace
            var log = new StreamWriter(logpath, true);
            writers.Add(new TextWriterWriteLine(log));

            ExecutableTools.LogArgs(new TextWriterWriteLine(log), args);

            var instance = new Instance(new Instance.Args
            {
                DbPath = dbpath,
                RestartDelay = config.Delay,
                Downloads = downloads,
                EndPoint = new IPEndPoint(IPAddress.Parse(config.IP), config.Port),
            });

            Stdio.SetStatus("Listening on {0}", instance.EndPoint);

            using (var disposer = new Disposer())
            {
                //wrap to add to disposable count
                disposer.Push(new Disposable.Wrapper(log));
                disposer.Push(instance);
                disposer.Push(() => Disposing(config.Timeout));

                if (config.Daemon)
                {
                    Logger.Trace("Stdin loop...");
                    var line = Stdio.ReadLine();
                    while (line != null)
                    {
                        Logger.Trace("> {0}", line);
                        if ("exit!" == line) break;
                        line = Stdio.ReadLine();
                    }
                    Logger.Trace("Stdin closed");
                }
                else
                {
                    Logger.Trace("Stdin loop...");
                    var shell = instance.CreateShell();
                    var stream = new ShellStream(new StdoutWriteLine(), new ConsoleReadLine());
                    //disposer.Push(stream); //stdin.readline wont return even after close/dispose call
                    var line = stream.ReadLine();
                    while (line != null)
                    {
                        if ("exit!" == line) break;
                        Shell.ParseAndExecute(shell, stream, line);
                        line = stream.ReadLine();
                    }
                    Logger.Trace("Stdin closed");
                }
            }

            Environment.Exit(0);
        }

        static void Disposing(int toms)
        {
            Logger.Trace("Launching dispose monitor");
            var thread = new Thread(() =>
            {
                var dl = DateTime.Now.AddMilliseconds(toms);
                Logger.Trace("Dispose monitor DL {0}", dl.ToString("HH:mm:ss.fff"));
                while (true)
                {
                    if (DateTime.Now > dl) throw ExceptionTools.Make("Timeout with dispose count {0}", Disposable.Undisposed);
                    if (Disposable.Undisposed == 0)
                    {
                        Logger.Trace("Dispose zero count detected");
                        Environment.Exit(0);
                    }
                    Thread.Sleep(100);
                }
            });
            thread.IsBackground = true;
            thread.Name = "DisposeMonitor";
            thread.Start();
        }
    }
}
