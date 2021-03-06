using System;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Text.RegularExpressions;
using NUnit.Framework;

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

        public Config(bool daemon = false)
        {
            Daemon = daemon;
            Root = ExecutableTools.Relative(@"Root");
            WebRoot = PathTools.Combine(Root, "Web");
            WebIP = "127.0.0.1";
            WebPort = 12334;
            ShellRoot = PathTools.Combine(Root, "Test_{0}", TimeTools.Compact(DateTime.Now));
            ShellIP = "127.0.0.1";
            ShellPort = 12333;

            Directory.CreateDirectory(WebRoot);
            Directory.CreateDirectory(ShellRoot);

            var writers = new WriteLineCollection();
            writers.Add(new StdoutWriteLine());
            var logfile = PathTools.Combine(Root, "log.txt");
            log = new StreamWriter(logfile, true);
            writers.Add(new TextWriterWriteLine(log));
            Timed = new TimedWriter(writers);
            Logger.TRACE = Timed;
            writers.WriteLine(string.Empty); //separating line
            writers.WriteLine("");
            Logger.Trace("-----------------------------------------------------------------------------");
            Logger.Trace("Test case {0} starting...", TestContext.CurrentContext.Test.FullName);
            //System.InvalidOperationException : This property has already been set and cannot be modified.
            //Thread.CurrentThread.Name = "NUnit";
        }

        public string WebEP { get { return $"{WebIP}:{WebPort}"; } }
        public string ShellEP { get { return $"{ShellIP}:{ShellPort}"; } }

        public void Dispose()
        {
            log.Dispose();
        }
    }

    public class Simple : IDisposable
    {
        private StreamWriter log;

        public Simple()
        {
            var writers = new WriteLineCollection();
            writers.Add(new StdoutWriteLine());
            var logfile = ExecutableTools.Relative("log.txt");
            log = new StreamWriter(logfile, true);
            writers.Add(new TextWriterWriteLine(log));
            Logger.TRACE = new TimedWriter(writers);
            writers.WriteLine(string.Empty); //separating line
            writers.WriteLine("");
            Logger.Trace("-----------------------------------------------------------------------------");
            Logger.Trace("Test case {0} starting...", TestContext.CurrentContext.Test.FullName);
        }

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
            //Logger.Trace("PUSH {0}", line);
            input.Push(line);
            //Logger.Trace("PUSHED {0}", line);
        }

        public void WaitFor(int toms, string format, params object[] args)
        {
            var dl = DateTime.Now.AddMilliseconds(toms);
            var pattern = TextTools.Format(format, args);
            while (true)
            {
                //Logger.Trace("POPPING for {0}", pattern);
                var line = input.Pop(1, null);
                //if (line == null) Logger.Trace("POP <NULL>");
                //if (line != null) Logger.Trace("POP {0}", line);
                //Beware | is regex reserved `or`
                if (line != null && Regex.IsMatch(line, pattern)) break;
                //if (DateTime.Now > dl) Logger.Trace("Timeout waiting for `{0}`", TextTools.Readable(pattern));
                if (DateTime.Now > dl) throw ExceptionTools.Make("Timeout waiting for `{0}`", TextTools.Readable(pattern));
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
        public static void Client(Config config, Action<ITestShell, string> action)
        {
            using (var disposer = new Disposer())
            {
                var client = SocketTools.ConnectWithTimeout(config.ShellIP, config.ShellPort, 2000);
                var endpoint = client.Client.LocalEndPoint as IPEndPoint;
                var stream = SocketTools.SslWithTimeout(client, 2000);
                var read = new StreamReader(stream);
                var write = new StreamWriter(stream);
                //var passfile = ExecutableTools.Relative("Password.txt");
                //var password = File.ReadAllText(passfile).Trim();
                //write.WriteLine(password);
                var shell = new TestShell();
                var reader = new Runner(new Runner.Args { ThreadName = "SOCKIN" });
                var writer = new Runner(new Runner.Args { ThreadName = "SOCKOUT" });
                reader.Run(() =>
                {
                    var line = read.ReadLine();
                    while (line != null)
                    {
                        Logger.Trace("<c {0}", line);
                        shell.WriteLine("<c {0}", line);
                        line = read.ReadLine();
                    }
                });
                writer.Run(() =>
                {
                    var line = shell.ReadLine();
                    while (line != null)
                    {
                        Logger.Trace("c> {0}", line);
                        write.WriteLine(line);
                        write.Flush();
                        line = shell.ReadLine();
                    }
                });
                disposer.Push(reader);
                disposer.Push(client);
                disposer.Push(writer);
                disposer.Push(shell);

                //netcore linux prints ipv6 in endpoint.tostring
                action(shell, $"{config.ShellIP}:{endpoint.Port}");
            }
        }

        public static void Shell(Config config, Action<ITestShell> action)
        {
            Directory.CreateDirectory(config.Root);
            var path = ExecutableTools.Relative("SharpDaemon.Server.exe");
            var args = $"Id=test Daemon={config.Daemon} Port={config.ShellPort} IP={config.ShellIP} Root=\"{config.ShellRoot}\"";
            Shell(path, args, action);
        }

        public static void Shell(string path, string args, Action<ITestShell> action)
        {
            using (var disposer = new Disposer())
            {
                var process = new DaemonProcess(new DaemonProcess.Args
                {
                    Executable = path,
                    Arguments = args,
                });

                Logger.Trace("Shell process {0} {1}", process.Id, process.Name, process.Info.FileName);
                Logger.Trace("Shell path {0}", process.Info.FileName);
                Logger.Trace("Shell args {0}", process.Info.Arguments);

                var shell = new TestShell();
                var reader = new Runner(new Runner.Args { ThreadName = "SDTOUT" });
                var erroer = new Runner(new Runner.Args { ThreadName = "STDERR" });
                var writer = new Runner(new Runner.Args { ThreadName = "STDIN" });
                reader.Run(() =>
                {
                    var line = process.ReadLine();
                    while (line != null)
                    {
                        Logger.Trace("<o {0}", line);
                        shell.WriteLine("<o {0}", line);
                        line = process.ReadLine();
                    }
                    shell.WriteLine(Environ.NewLines);
                });
                erroer.Run(() =>
                {
                    var line = process.ReadError();
                    while (line != null)
                    {
                        Logger.Trace("<e {0}", line);
                        shell.WriteLine("<e {0}", line);
                        line = process.ReadError();
                    }
                });
                writer.Run(() =>
                {
                    var line = shell.ReadLine();
                    while (line != null)
                    {
                        Logger.Trace("i> {0}", line);
                        process.WriteLine(line);
                        line = shell.ReadLine();
                    }
                });
                disposer.Push(erroer);
                disposer.Push(reader);
                disposer.Push(process);
                disposer.Push(writer);
                disposer.Push(shell);

                action(shell);
            }
        }

        public static void Web(Config config, Action action)
        {
            Directory.CreateDirectory(config.WebRoot);
            var zippath = PathTools.Combine(config.WebRoot, "Daemon.StaticWebServer.zip");
            if (File.Exists(zippath)) File.Delete(zippath);
            Logger.Trace("Zipping to {0}", zippath);
            ZipTools.ZipFromFiles(zippath, ExecutableTools.Directory()
                , "Daemon.StaticWebServer.exe"
                , "SharpDaemon.dll"
            );
            var process = new DaemonProcess(new DaemonProcess.Args
            {
                Executable = ExecutableTools.Relative("Daemon.StaticWebServer.exe"),
                Arguments = $"EndPoint={config.WebEP} Root=\"{config.WebRoot}\"",
            });
            Logger.Trace("Web process {0} {1} {2} {3}", process.Id, process.Name, process.Info.FileName, process.Info.Arguments);
            var reader = new Runner(new Runner.Args { ThreadName = "WEB" });
            reader.Run(() =>
            {
                var line = process.ReadLine();
                while (line != null)
                {
                    Logger.Trace("<w {0}", line);
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
    }
}
