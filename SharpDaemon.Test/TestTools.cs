using System;
using System.IO;
using System.Text.RegularExpressions;

namespace SharpDaemon.Test
{
    class Config : IDisposable
    {
        public string Root;
        public string ShellRoot;
        public string WebRoot;
        public string WebIP;
        public int WebPort;
        public string ShellIP;
        public int ShellPort;
        public bool Daemon;

        public IWriteLine Timed;
        private StreamWriter log;

        public Config()
        {
            Root = ExecutableTools.Relative(@"WS");
            WebRoot = ExecutableTools.Relative(@"WS\Web");
            WebIP = "127.0.0.1";
            WebPort = 12334;
            ShellRoot = ExecutableTools.Relative(@"WS\WS_{0}", TimeTools.Compact(DateTime.Now));
            ShellIP = "127.0.0.1";
            ShellPort = 12333;

            Directory.CreateDirectory(WebRoot);
            Directory.CreateDirectory(ShellRoot);

            var writers = new WriteLineCollection();
            writers.Add(new ConsoleWriteLine());
            var logfile = PathTools.Combine(Root, "log.txt");
            log = new StreamWriter(logfile, true);
            writers.Add(new TextWriterWriteLine(log));
            Timed = new TimedWriter(writers);
            Output.TRACE = new NamedOutput(Timed, "TRACE");
        }

        public string WebEP { get { return $"{WebIP}:{WebPort}"; } }
        public string ShellEP { get { return $"{ShellIP}:{ShellPort}"; } }

        public void Dispose()
        {
            log.Dispose();
        }
    }

    interface ITestShell
    {
        void Execute(string line);
        void WaitFor(int toms, string format, params object[] args);
    }

    class TestShell : IWriteLine, IReadLine, ITestShell, IDisposable
    {
        private readonly LockedQueue<string> input = new LockedQueue<string>();
        private readonly LockedQueue<string> output = new LockedQueue<string>();
        private volatile bool disposed;

        public void WriteLine(string format, params object[] args)
        {
            var line = TextTools.Format(format, args);
            input.Push(line);
        }

        public void WaitFor(int toms, string format, params object[] args)
        {
            var dl = DateTime.Now.AddMilliseconds(toms);
            var pattern = TextTools.Format(format, args);
            while (true)
            {
                var line = input.Pop(1, null);
                //if (line != null) Stdio.WriteLine("POP {0}", line);
                //Beware | is regex reserved `or`
                if (line != null && Regex.IsMatch(line, pattern)) break;
                if (DateTime.Now > dl) throw ExceptionTools.Make("Timeout waiting for `{0}`", pattern);
            }
        }

        public void Execute(string line)
        {
            output.Push(line);
        }

        public string ReadLine()
        {
            while (true)
            {
                var line = output.Pop(1, null);
                if (line != null) return line;
                if (disposed) return null;
            }
        }

        public void Dispose()
        {
            disposed = true;
        }
    }

    class TestTools
    {
        public static void Web(Config config, Action action)
        {
            var output = new NamedOutput(config.Timed, "WEB");
            Directory.CreateDirectory(config.WebRoot);
            var webserver = ExecutableTools.Relative(@"Daemon.StaticWebServer.exe");
            var zippath = PathTools.Combine(config.WebRoot, "Daemon.StaticWebServer.zip");
            if (!File.Exists(zippath))
            {
                output.WriteLine("Zipping to {0}", zippath);
                ZipTools.ZipFromFiles(zippath, ExecutableTools.Directory()
                    , "Daemon.StaticWebServer.exe"
                    , "SharpDaemon.dll"
                    , "Nancy.Hosting.Self.dll"
                    , "Nancy.dll"
                );
            }
            var process = new DaemonProcess(new DaemonProcess.Args
            {
                Executable = webserver,
                Arguments = $"EndPoint={config.WebEP} Root=\"{config.WebRoot}\"",
            });
            var reader = new Runner();
            reader.Run(() =>
            {
                var line = process.ReadLine();
                while (line != null)
                {
                    output.WriteLine("< {0}", line);
                    line = process.ReadLine();
                }
            });
            using (reader)
            {
                using (process)
                {
                    action();
                }
            }
        }

        public static void Run(Config config, Action<ITestShell> test)
        {
            using (var disposer = new Disposer())
            {
                Directory.CreateDirectory(config.Root);

                var process = new DaemonProcess(new DaemonProcess.Args
                {
                    Executable = ExecutableTools.Relative("SharpDaemon.Server.exe"),
                    Arguments = $"Id=test Daemon={config.Daemon} Port={config.ShellPort} IP={config.ShellIP} Root=\"{config.ShellRoot}\"",
                });

                var output = new Output(config.Timed);
                var named = new NamedOutput(output, "PROCESS");
                var shell = new TestShell();
                var reader = new Runner();
                var writer = new Runner();
                reader.Run(() =>
                {
                    var line = process.ReadLine();
                    while (line != null)
                    {
                        named.WriteLine(line);
                        shell.WriteLine(line);
                        line = process.ReadLine();
                    }
                });
                writer.Run(() =>
                {
                    var line = shell.ReadLine();
                    while (line != null)
                    {
                        process.WriteLine(line);
                        line = shell.ReadLine();
                    }
                });
                disposer.Push(reader);
                disposer.Push(process);
                disposer.Push(writer);
                disposer.Push(shell);

                test(shell);
            }
        }
    }
}