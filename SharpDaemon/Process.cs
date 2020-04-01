using System;
using System.Diagnostics;

namespace SharpDaemon
{
    public class ProcessException : Exception
    {
        private readonly string trace;

        public ProcessException(string message, string trace) : base(message)
        {
            this.trace = trace;
        }

        public string Trace { get { return trace; } }
    }

    public class DaemonProcess : Disposable
    {
        private readonly Action<Exception> handler;
        private readonly Process process;
        private readonly DateTime start;
        private readonly int id;
        private readonly string name;

        public class Args
        {
            public string Executable { get; set; }
            public string Arguments { get; set; }
            public Action<Exception> ExceptionHandler { get; set; }
        }

        public DaemonProcess(Args args)
        {
            handler = args.ExceptionHandler;
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
            Tools.Try(() =>
            {
                process.StandardInput.WriteLine();
                process.StandardInput.Flush();
                process.WaitForExit(200);
            });
            Tools.Try(process.Kill);
            Tools.Try(process.Dispose);
        }

        public string ReadLine()
        {
            var line = process.StandardOutput.ReadLine();
            if (line != null && line.StartsWith("!"))
            {
                var trace = process.StandardOutput.ReadToEnd();
                return string.Format("{0}\n{1}", line, trace);
            }
            return line;
        }

        public void WriteLine(string format, params object[] args)
        {
            process.StandardInput.WriteLine(format, args);
            process.StandardInput.Flush();
        }
    }
}
