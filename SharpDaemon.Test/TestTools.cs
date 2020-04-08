using System;
using System.IO;
using System.Text.RegularExpressions;

namespace SharpDaemon.Test
{
    //Testing Environment Setup
    //In Shell $1
    // .\DebugDaemons.bat
    //In Shell #2 (as admin for advanced testing)
    // run $env:REPO=($pwd).path (set REPO=%CD%)
    // dotnet test SharpDaemon.Test
    class Env
    {
        public string Repo;
        public string WebEP;
        public string WebRoot;

        public Env()
        {
            Repo = Environment.GetEnvironmentVariable("REPO");
            WebRoot = $@"{Repo}\Output";
            WebEP = "127.0.0.1:12334";
        }
    }

    interface ITestShell
    {
        void Execute(params string[] tokens);
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

        public void Execute(params string[] tokens)
        {
            var line = string.Join(" ", tokens);
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
        public static void Run(bool daemon, Action<Env, ITestShell> test)
        {
            using (var disposer = new Disposer())
            {
                var env = new Env();

                var writers = new WriteLineCollection();
                writers.Add(new ConsoleWriteLine());
                var log = new StreamWriter(ExecutableTools.Relative("log.txt"), true);
                disposer.Push(log);
                writers.Add(new TextWriterWriteLine(log));
                var timed = new TimedWriter(writers);
                Disposable.Debug = new NamedOutput(timed, "DISPOSE");

                var port = 12333;
                var ip = "127.0.0.1";
                var root = ExecutableTools.Relative("WS_{0}", TimeTools.Compact(DateTime.Now));

                var process = new DaemonProcess(new DaemonProcess.Args
                {
                    Executable = ExecutableTools.Relative("SharpDaemon.Server.exe"),
                    Arguments = $"Id=test Daemon={daemon} Port={port} IP={ip} Root=\"{root}\"",
                });

                var output = new Output(timed);
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

                test(env, shell);
            }
        }
    }
}
