using System;

namespace SharpDaemon.Server
{
    public partial class Controller
    {
        class DaemonRT : Disposable
        {
            private readonly DaemonProcess process;
            private readonly Runner runner;
            private readonly Output output;
            private readonly DaemonDto dto;
            private volatile string status;
            private DateTime? restart;

            public string Id { get { return dto.Id; } }
            public DaemonDto Dto { get { return dto.Clone(); } }

            public DaemonRT(DaemonDto dto, string root, Output output, Action<Exception> handler)
            {
                using (var disposer = new Disposer())
                {
                    this.output = output;
                    this.dto = dto;
                    status = "Starting...";
                    //push must match dispose order
                    runner = new Runner(new Runner.Args
                    {
                        ThreadName = dto.Id,
                        ExceptionHandler = handler,
                    });
                    disposer.Push(runner);
                    process = new DaemonProcess(new DaemonProcess.Args
                    {
                        Executable = Tools.Absolute(root, dto.Path),
                        Arguments = dto.Args,
                    });
                    disposer.Push(process);
                    runner.Run(ReadLoop);
                    disposer.Clear();
                }
            }

            protected override void Dispose(bool disposed)
            {
                Tools.Try(process.Dispose);
                Tools.Try(runner.Dispose);
            }

            public bool NeedRestart()
            {
                return restart.HasValue && DateTime.Now > restart.Value;
            }

            public void UpdateRestart(int delay)
            {
                restart = DateTime.Now.AddMilliseconds(delay);
            }

            public void UpdateStatus(string status) => this.status = status;

            public void Run(Action action) => runner.Run(action);

            public string FullInfo() => string.Format("{0}|{1}|{2}|{3}", dto.Id, process.Id, process.Name, status);

            public string ProcessInfo() => string.Format("{0}|{1}", process.Name, process.Id);

            private void ReadLoop()
            {
                var line = process.ReadLine();
                while (line != null)
                {
                    status = line;
                    output.Output("Daemon {0} {1} < {2}", dto.Id, ProcessInfo(), line);
                    line = process.ReadLine();
                }
            }
        }
    }
}