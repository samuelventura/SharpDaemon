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
            private Output output;
            private TcpClient client;
            private IPEndPoint endpoint;
            private WriterOutput writer;
            private StreamReader reader;
            private DateTime start;
            private DateTime last;
            private Runner runner;
            private Shell shell;

            public IPEndPoint EndPoint { get { return endpoint; } }

            public ClientRt(TcpClient client, Output output, ShellFactory factory, Action<Exception> handler)
            {
                //client disposed in caller
                using (var disposer = new Disposer())
                {
                    this.output = output;
                    this.client = client;
                    start = DateTime.Now;
                    last = DateTime.Now;
                    shell = factory.Create();
                    var stream = client.GetStream();
                    reader = new StreamReader(stream, Encoding.UTF8);
                    writer = new WriterOutput(new StreamWriter(stream, Encoding.UTF8));
                    endpoint = client.Client.RemoteEndPoint as IPEndPoint;
                    runner = new Runner(new Runner.Args
                    {
                        ExceptionHandler = handler,
                        ThreadName = string.Format("Client_{0}", endpoint),
                    });
                    disposer.Push(runner);
                    runner.Run(ReadLoop);
                    disposer.Clear();
                }
            }

            protected override void Dispose(bool disposed)
            {
                Tools.Try(client.Close);
                Tools.Try(runner.Dispose);
            }

            public void Run(Action action) => runner.Run(action);

            public string Info() => string.Format("{0}|{1}|{2}", endpoint, Tools.Format(start), IdleTime());

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
                    output.Output("Client {0} < {1}", endpoint, line);
                    var tokens = Tools.Tokens(line, writer);
                    if (tokens != null && tokens.Length > 0) shell.Execute(writer, tokens);
                    line = reader.ReadLine();
                }
            }
        }
    }
}