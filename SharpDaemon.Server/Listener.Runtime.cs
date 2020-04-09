using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace SharpDaemon.Server
{
    public partial class Listener
    {
        class ClientRt : Disposable
        {
            private readonly IPEndPoint endpoint;
            private readonly IOutput output;
            private readonly TcpClient client;
            private readonly Runner runner;
            private readonly Shell shell;
            private DateTime start;
            private DateTime last;

            public IPEndPoint EndPoint { get { return endpoint; } }

            public ClientRt(TcpClient client, IWriteLine writer, ShellFactory factory)
            {
                //client disposed in caller on throw
                this.client = client;
                using (var disposer = new Disposer())
                {
                    start = DateTime.Now;
                    last = DateTime.Now;
                    shell = factory.Create();
                    endpoint = client.Client.RemoteEndPoint as IPEndPoint;
                    var name = string.Format("Client_{0}", endpoint);
                    output = new NamedOutput(writer, name);
                    runner = new Runner(new Runner.Args
                    {
                        ExceptionHandler = output.HandleException,
                        ThreadName = name,
                    });
                    disposer.Push(runner);

                    disposer.Push(Dispose); //ensure cleanup order
                    runner.Run(ReadLoop);
                    disposer.Clear();
                }
            }

            protected override void Dispose(bool disposed)
            {
                //no need to close stream (writer and reader)
                ExceptionTools.Try(client.Close);
                runner.Dispose();
            }

            public void Run(Action action) => runner.Run(action);

            public string Info(string format)
            {
                var parts = format.Split('|');
                for (var i = 0; i < parts.Length; i++)
                {
                    switch (parts[i])
                    {
                        case "IP": parts[i] = endpoint.Address.ToString(); break;
                        case "Port": parts[i] = endpoint.Port.ToString(); break;
                        case "Endpoint": parts[i] = endpoint.ToString(); break;
                        case "Start": parts[i] = TimeTools.Format(start); break;
                        case "Idle": parts[i] = IdleTime(); break;
                    }
                }
                return string.Join("|", parts);
            }

            private string IdleTime()
            {
                var idle = DateTime.Now - last;
                return TimeTools.Format(idle.TotalSeconds);
            }

            private void ReadLoop()
            {
                using (client)
                {
                    var stream = client.GetStream();
                    var reader = new TextReaderReadLine(new StreamReader(stream));
                    var output = new Output(new TextWriterWriteLine(new StreamWriter(stream)));
                    ReadLoop(client, new ShellStream(output, reader));
                }
            }

            private void ReadLoop(TcpClient client, ShellStream stream)
            {
                using (stream) //shell stream runner last
                using (client) //client first
                {
                    var line = stream.ReadLine();
                    while (line != null)
                    {
                        last = DateTime.Now;
                        output.WriteLine("< {0}", line);
                        Shell.ParseAndExecute(shell, stream, line);
                        line = stream.ReadLine();
                    }
                }
            }
        }
    }
}