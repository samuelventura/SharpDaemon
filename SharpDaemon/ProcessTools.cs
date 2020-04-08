using System;
using System.Diagnostics;

namespace SharpDaemon
{
    public class DaemonProcess : Disposable, IReadLine, IWriteLine
    {
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
            process.StartInfo = new ProcessStartInfo()
            {
                FileName = args.Executable,
                Arguments = args.Arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = false,
            };
            process.Start();
            id = process.Id;
            name = process.ProcessName;
            start = DateTime.Now;
        }

        public int Id { get { return id; } }
        public string Name { get { return name; } }
        public DateTime Start { get { return start; } }

        protected override void Dispose(bool disposed)
        {
            ExceptionTools.Try(() =>
            {
                process.StandardInput.Close();
                process.WaitForExit(wait > 0 ? wait : 5000);
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
            using (var disposer = new Disposer())
            {
                var reader = new Runner(new Runner.Args() { ExceptionHandler = stream.HandleException, });
                disposer.Push(reader);
                var process = new DaemonProcess(args);
                stream.WriteLine("Process {0} created", process.Id);
                disposer.Push(process);
                var queue = new LockedQueue<bool>();
                reader.Run(() =>
                {
                    var line = process.ReadLine();
                    while (line != null)
                    {
                        stream.WriteLine(line);
                        line = process.ReadLine();
                    }
                });
                reader.Run(() => queue.Push(true));
                while (true)
                {
                    var line = stream.TryReadLine(out var eof);
                    if (eof) Disposable.Trace("EOF");
                    if (eof) return;
                    if (line != null) process.WriteLine(line);
                    if (queue.Pop(1, false)) return;
                }
            }
        }
    }
}
