using System;

namespace SharpDaemon.Server
{
    public partial class Manager
    {
        class DaemonRT : Disposable
        {
            private readonly DaemonProcess process;
            private readonly Runner reader;
            private readonly Runner erroer;
            private readonly DaemonDto dto;
            private volatile string status;
            private readonly int delay;
            private DateTime? restart;

            public string Id { get { return dto.Id; } }
            public int Pid { get { return process.Id; } }
            public DaemonDto Dto { get { return dto.Clone(); } }

            public DaemonRT(DaemonDto dto, string root, int delay)
            {
                this.delay = delay;
                this.dto = dto.Clone();

                using (var disposer = new Disposer())
                {
                    process = new DaemonProcess(new DaemonProcess.Args
                    {
                        Executable = PathTools.Combine(root, dto.Path),
                        //id matches [a-zA_Z][a-zA_Z0-9_]*
                        Arguments = string.Format("Id={0} Daemon=True {1}", dto.Id, dto.Args),
                    });
                    disposer.Push(process);

                    status = "Starting...";

                    reader = new Runner(new Runner.Args { ThreadName = string.Format("DAEMON_{0}_{1}_O", dto.Id, process.Id) });
                    erroer = new Runner(new Runner.Args { ThreadName = string.Format("DAEMON_{0}_{1}_E", dto.Id, process.Id) });
                    disposer.Push(erroer);

                    disposer.Push(Dispose); //ensure cleanup order
                    erroer.Run(ErrorLoop);
                    reader.Run(ReadLoop);
                    reader.Run(UpdateRestart);
                    disposer.Clear();
                }
            }

            protected override void Dispose(bool disposed)
            {
                ExceptionTools.Try(process.Dispose);
                ExceptionTools.Try(reader.Dispose);
            }

            public bool WillRestart()
            {
                return restart.HasValue;
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
                    Logger.Trace("<o {0}", line);
                    line = process.ReadLine();
                }
            }

            private void ErrorLoop()
            {
                var line = process.ReadError();
                while (line != null)
                {
                    Logger.Trace("<e {0}", line);
                    line = process.ReadError();
                }
            }
        }
    }
}