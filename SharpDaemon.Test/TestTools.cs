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
            writers.Add(new ConsoleWriteLine());
            var logfile = PathTools.Combine(Root, "log.txt");
            log = new StreamWriter(logfile, true);
            writers.Add(new TextWriterWriteLine(log));
            Timed = new TimedWriter(writers);
            var pid = Process.GetCurrentProcess().Id;
            Output.TRACE = new NamedOutput(Timed, string.Format("TRACE_{0}", pid));
            writers.WriteLine(string.Empty); //separating line
            Output.Trace("Test case {0} starting...", TestContext.CurrentContext.Test.FullName);
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
        public static void Client(Config config, Action<ITestShell, string> action)
        {
            using (var disposer = new Disposer())
            {
                var client = SocketTools.ConnectWithTimeout(config.ShellIP, config.ShellPort, 2000);
                var endpoint = client.Client.LocalEndPoint as IPEndPoint;
                var stream = SocketTools.SslWithTimeout(client, 2000);
                var read = new StreamReader(stream);
                var write = new StreamWriter(stream);
                var output = new Output(config.Timed);
                var named = new NamedOutput(output, string.Format("SOCKET_{0} <", endpoint));
                //var passfile = ExecutableTools.Relative("Password.txt");
                //var password = File.ReadAllText(passfile).Trim();
                //write.WriteLine(password);
                var shell = new TestShell();
                var reader = new Runner();
                var writer = new Runner();
                reader.Run(() =>
                {
                    var line = read.ReadLine();
                    while (line != null)
                    {
                        named.WriteLine(line);
                        shell.WriteLine(line);
                        line = read.ReadLine();
                    }
                });
                writer.Run(() =>
                {
                    var line = shell.ReadLine();
                    while (line != null)
                    {
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
            using (var disposer = new Disposer())
            {
                Directory.CreateDirectory(config.Root);

                var server = ExecutableTools.Relative("SharpDaemon.Server.dll");
                var process = new DaemonProcess(new DaemonProcess.Args
                {
                    Executable = Environ.Executable("dotnet"),
                    Arguments = $"\"{server}\" Id=test Daemon={config.Daemon} Port={config.ShellPort} IP={config.ShellIP} Root=\"{config.ShellRoot}\"",
                });
                Output.Trace("Shell process {0} {1} {2} {3}", process.Id, process.Name, process.Info.FileName, process.Info.Arguments);
                var output = new Output(config.Timed);
                var named = new NamedOutput(output, string.Format("STDIO_{0} <", process.Id));
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

                action(shell);
            }
        }

        public static void Web(Config config, Action action)
        {
            var output = new NamedOutput(config.Timed, "WEB");
            Directory.CreateDirectory(config.WebRoot);
            //Output.Trace("Executable Directory {0}", ExecutableTools.Directory());
            var webserver = ExecutableTools.Relative("Daemon.StaticWebServer.dll");
            var zippath = PathTools.Combine(config.WebRoot, "Daemon.StaticWebServer.zip");
            if (File.Exists(zippath)) File.Delete(zippath);
            output.WriteLine("Zipping to {0}", zippath);
            ZipTools.ZipFromFiles(zippath, ExecutableTools.Directory()
                , "Daemon.StaticWebServer.runtimeconfig.json"
                , "Daemon.StaticWebServer.dll"
                , "SharpDaemon.dll"
            );
            var process = new DaemonProcess(new DaemonProcess.Args
            {
                Executable = Environ.Executable("dotnet"),
                Arguments = $"\"{webserver}\" EndPoint={config.WebEP} Root=\"{config.WebRoot}\"",
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
    }
}
