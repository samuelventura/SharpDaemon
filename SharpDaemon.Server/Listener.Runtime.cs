using System;
using System.IO;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace SharpDaemon.Server
{
    public partial class Listener
    {
        class ClientRt : Disposable
        {
            private readonly X509Certificate2 certificate;
            private readonly IPEndPoint endpoint;
            private readonly IOutput output;
            private readonly TcpClient client;
            private readonly Runner runner;
            private readonly Shell shell;
            private DateTime start;
            private DateTime last;

            public IPEndPoint EndPoint { get { return endpoint; } }

            public ClientRt(TcpClient client, X509Certificate2 certificate, IWriteLine writer, ShellFactory factory)
            {
                //client disposed in caller on throw
                this.client = client;
                this.certificate = certificate;
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
                    var stream = new SslStream(client.GetStream());
                    stream.AuthenticateAsServer(certificate);
                    var reader = new TextReaderReadLine(new StreamReader(stream));
                    var writer = new TextWriterWriteLine(new StreamWriter(stream));
                    if (!endpoint.ToString().StartsWith("127.0.0.1"))
                    {
                        var passfile = ExecutableTools.Relative("Password.txt");
                        var password = File.ReadAllText(passfile).Trim();
                        if (password != reader.ReadLine()) return;
                    }
                    var output = new Output(writer);
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