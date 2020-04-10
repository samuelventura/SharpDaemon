using System;
using System.Text;
using System.Diagnostics;

namespace SharpDaemon
{
    public class DaemonProcess : Disposable, IReadLine, IWriteLine
    {
        private readonly ProcessStartInfo info;
        private readonly Process process;
        private readonly DateTime start;
        private readonly int id;
        private readonly string name;
        private readonly int wait;

        public class Args
        {
            public string Executable { get; set; }
            public string Arguments { get; set; }
            public int KillDelay { get; set; }
        }

        public DaemonProcess(Args args)
        {
            wait = args.KillDelay;
            process = new Process();
            info = new ProcessStartInfo()
            {
                FileName = args.Executable,
                Arguments = args.Arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = false,
            };
            process.StartInfo = info;
            process.Start();
            id = process.Id;
            name = process.ProcessName;
            start = DateTime.Now;
        }

        public int Id { get { return id; } }
        public string Name { get { return name; } }
        public DateTime Start { get { return start; } }
        public ProcessStartInfo Info { get { return info; } }

        protected override void Dispose(bool disposed)
        {
            ExceptionTools.Try(() =>
            {
                process.StandardInput.Close();
                var to = wait > 0 ? wait : 5000;
                Output.Trace("WaitForExit {0} {1}ms...", id, to);
                var start = DateTime.Now;
                var exited = process.WaitForExit(to);
                var elapsed = DateTime.Now - start;
                Output.Trace("WaitForExit {0} {1:0}ms {2}", id, elapsed.TotalMilliseconds, exited);
            });
            ExceptionTools.Try(process.Kill);
            ExceptionTools.Try(process.Dispose);
        }

        public string ReadLine()
        {
            return process.StandardOutput.ReadLine();
        }

        public void WriteLine(string format, params object[] args)
        {
            process.StandardInput.WriteLine(format, args);
            process.StandardInput.Flush();
        }

        public static void Interactive(IStream stream, Args args)
        {
            var reader = new Runner(new Runner.Args() { ExceptionHandler = stream.HandleException, });
            using (reader)
            {
                var process = new DaemonProcess(args);
                //eof below must close process immediatelly
                //to ensure exited message happens after real exit
                using (process)
                {
                    stream.WriteLine("Process {0} has started", process.Id);
                    var queue = new LockedQueue<bool>();
                    reader.Run(() =>
                    {
                        var line = process.ReadLine();
                        while (line != null)
                        {
                            stream.WriteLine(line);
                            line = process.ReadLine();
                        }
                        Output.Trace("EOF output < process");
                    });
                    reader.Run(() => queue.Push(true));
                    while (true)
                    {
                        //non blocking readline needed to notice reader exit
                        var line = stream.TryReadLine(out var eof);
                        if (eof) Output.Trace("EOF input > process");
                        if (eof) break;
                        if (line != null) process.WriteLine(line);
                        if (queue.Pop(1, false)) break;
                    }
                }
                //previous loop may swallow exit! by feeding it to process
                //unit test should wait for syncing message below before exit!
                stream.WriteLine("Process {0} has exited", process.Id);
            }
        }

        public static string MakeCli(string[] args, int offset)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < args.Length - offset; i++)
            {
                //reject arguments with double quotes
                //double quote arguments with spaces
                var arg = args[i + offset];
                if (sb.Length > 0) sb.Append(" ");
                AssertTools.True(!arg.Contains("\""), "Invalid arg {0} {1}", i, arg);
                if (arg.Contains(" ")) sb.Append("\"");
                sb.Append(arg);
                if (arg.Contains(" ")) sb.Append("\"");
            }
            return sb.ToString();
        }
    }
}
