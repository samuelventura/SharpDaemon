using System;

namespace SharpDaemon.Server
{
    public partial class Manager
    {
        class DaemonRT : Disposable
        {
            private readonly DaemonProcess process;
            private readonly NamedOutput named;
            private readonly Runner runner;
            private readonly DaemonDto dto;
            private volatile string status;
            private readonly int delay;
            private DateTime? restart;

            public string Id { get { return dto.Id; } }
            public int Pid { get { return process.Id; } }
            public DaemonDto Dto { get { return dto.Clone(); } }

            public DaemonRT(DaemonDto dto, string root, int delay, Output output)
            {
                this.delay = delay;
                this.dto = dto.Clone();

                using (var disposer = new Disposer())
                {
                    process = new DaemonProcess(new DaemonProcess.Args
                    {
                        Executable = Tools.Combine(root, dto.Path),
                        Arguments = dto.Args,
                    });
                    disposer.Push(process);

                    status = "Starting...";
                    var name = string.Format("{0}_{1}", dto.Id, process.Id);
                    named = new NamedOutput(name, output);

                    runner = new Runner(new Runner.Args
                    {
                        ExceptionHandler = named.OnException,
                        ThreadName = name,
                    });
                    disposer.Push(runner);

                    disposer.Push(Dispose); //ensure cleanup order
                    runner.Run(ReadLoop);
                    runner.Run(UpdateRestart);
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

            public void UpdateRestart()
            {
                status = "Restarting...";
                restart = DateTime.Now.AddMilliseconds(delay);
            }

            public string Info(string format)
            {
                var parts = format.Split('|');
                for (var i = 0; i < parts.Length; i++)
                {
                    switch (parts[i])
                    {
                        case "Id": parts[i] = dto.Id; break;
                        case "Path": parts[i] = dto.Path; break;
                        case "Args": parts[i] = dto.Args; break;
                        case "Pid": parts[i] = process.Id.ToString(); break;
                        case "Name": parts[i] = process.Name; break;
                        case "Status": parts[i] = status; break;
                    }
                }
                return string.Join("|", parts);
            }

            private void ReadLoop()
            {
                var line = process.ReadLine();
                while (line != null)
                {
                    status = line;
                    named.WriteLine("< {0}", line);
                    line = process.ReadLine();
                }
            }
        }
    }
}