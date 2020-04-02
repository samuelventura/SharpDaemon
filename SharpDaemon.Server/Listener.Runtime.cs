using System;
using System.IO;
using System.Net;
using System.Text;
using System.Net.Sockets;

namespace SharpDaemon.Server
{
    public partial class Listener
    {
        class ClientRt : Disposable
        {
            private NamedOutput named;
            private TcpClient client;
            private IPEndPoint endpoint;
            private WriterOutput writer;
            private StreamReader reader;
            private DateTime start;
            private DateTime last;
            private Runner runner;
            private Shell shell;

            public IPEndPoint EndPoint { get { return endpoint; } }

            public ClientRt(TcpClient client, Output output, ShellFactory factory)
            {
                //client disposed in caller on throw
                this.client = client;
                using (var disposer = new Disposer())
                {
                    start = DateTime.Now;
                    last = DateTime.Now;
                    shell = factory.Create();
                    var stream = client.GetStream();
                    reader = new StreamReader(stream);
                    writer = new WriterOutput(new StreamWriter(stream));
                    endpoint = client.Client.RemoteEndPoint as IPEndPoint;
                    var name = string.Format("Client_{0}", endpoint);
                    named = new NamedOutput(name, output);
                    runner = new Runner(new Runner.Args
                    {
                        ExceptionHandler = named.OnException,
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
                Tools.Try(client.Close);
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
                        case "Start": parts[i] = Tools.Format(start); break;
                        case "Idle": parts[i] = IdleTime(); break;
                    }
                }
                return string.Join("|", parts);
            }
            private string IdleTime()
            {
                var idle = DateTime.Now - last;
                return Tools.Format(idle.TotalSeconds);
            }

            private void ReadLoop()
            {
                var line = reader.ReadLine();
                while (line != null)
                {
                    last = DateTime.Now;
                    named.WriteLine("< {0}", line);
                    var tokens = Tools.Tokens(line, writer);
                    if (tokens != null && tokens.Length > 0) shell.Execute(writer, tokens);
                    line = reader.ReadLine();
                }
            }
        }
    }
}