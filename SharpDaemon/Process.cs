using System;
using System.Diagnostics;

namespace SharpDaemon
{
    public class DaemonProcess : Disposable
    {
        private readonly Process process;
        private readonly DateTime start;
        private readonly int id;
        private readonly string name;

        public class Args
        {
            public string Executable { get; set; }
            public string Arguments { get; set; }
        }

        public DaemonProcess(Args args)
        {
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
            return process.StandardOutput.ReadLine();
        }

        public void WriteLine(string format, params object[] args)
        {
            process.StandardInput.WriteLine(format, args);
            process.StandardInput.Flush();
        }
    }
}
