using System;
using System.Diagnostics;

namespace SharpDaemon
{
    public class DaemonProcess : IDisposable
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

        public void Dispose()
        {
            Tools.Try(() =>
            {
                process.StandardInput.WriteLine();
                process.StandardInput.Flush();
                process.WaitForExit(200);
            }, handler);
            Tools.Try(process.Kill, handler);
            Tools.Try(process.Dispose, handler);
        }

        public string ReadLine()
        {
            var line = process.StandardOutput.ReadLine();
            if (line == null) throw Tools.Make("Daemon process EOF");
            if (line.StartsWith("!"))
            {
                var trace = process.StandardOutput.ReadToEnd();
                throw new ProcessException(line.Substring(1), trace);
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
