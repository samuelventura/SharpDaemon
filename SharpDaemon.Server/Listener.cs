using System;
using System.IO;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Collections.Generic;

namespace SharpDaemon.Server
{
    public class Listener : Disposable, IScriptable
    {
        private readonly ShellFactory factory;
        private readonly HashSet<ClientRt> clients;
        private readonly Action<Exception> handler;
        private readonly IPEndPoint endpoint;
        private readonly TcpListener server;
        private readonly Runner accepter;
        private readonly Runner register;
        private readonly Output output;

        public class Args
        {
            public int TcpPort { get; set; }
            public string IpAddress { get; set; }
            public Output Output { get; set; }
            public ShellFactory ShellFactory { get; set; }
            public Action<Exception> ExceptionHandler { get; set; }
        }

        public IPEndPoint EndPoint { get { return endpoint; } }

        public Listener(Args args)
        {
            factory = args.ShellFactory;
            handler = args.ExceptionHandler;
            clients = new HashSet<ClientRt>();
            output = new NamedOutput("LISTENER", args.Output);
            server = new TcpListener(IPAddress.Parse(args.IpAddress), args.TcpPort);
            using (var disposer = new Disposer(handler))
            {
                disposer.Push(server.Stop);
                //daemons prevent stop from exiting accepter
                server.MakeNotInheritable();
                server.Start();
                endpoint = server.LocalEndpoint as IPEndPoint;
                register = new Runner(new Runner.Args
                {
                    ExceptionHandler = handler,
                    ThreadName = "Register",
                });
                disposer.Push(register);
                accepter = new Runner(new Runner.Args
                {
                    ExceptionHandler = handler,
                    ThreadName = "Accepter",
                });
                disposer.Push(accepter);
                //no need to add stop to the end runners 
                //will dispose because not accepting yet
                disposer.Clear();
            }
        }

        public void Start()
        {
            accepter.Run(AcceptLoop);
        }

        protected override void Dispose(bool disposed)
        {
            Tools.Try(server.Stop);
            Tools.Try(accepter.Dispose);
            register.Dispose(() =>
            {
                foreach (var rt in clients) Tools.Try(rt.Dispose);
                clients.Clear();
            });
        }

        private void AcceptLoop()
        {
            while (true) AcceptTcpClient();
        }

        private void AcceptTcpClient()
        {
            var client = server.AcceptTcpClient();
            register.Run(() =>
            {
                using (var disposer = new Disposer(handler))
                {
                    disposer.Push(client);
                    var stream = client.GetStream();
                    var endpoint = client.Client.RemoteEndPoint as IPEndPoint;
                    output.Output("Client {0} connected", endpoint);
                    var runner = new Runner(new Runner.Args { ExceptionHandler = handler });
                    disposer.Push(runner);
                    var rt = new ClientRt
                    {
                        Output = output,
                        Runner = runner,
                        EndPoint = endpoint,
                        TcpClient = client,
                        Start = DateTime.Now,
                        Shell = factory.Create(),
                        Reader = new StreamReader(stream, Encoding.UTF8),
                        Writer = new WriterOutput(new StreamWriter(stream, Encoding.UTF8)),
                    };
                    //ensure cleanup order
                    disposer.Push(rt);
                    clients.Add(rt);
                    runner.Run(rt.ReadLoop);
                    runner.Run(() => output.Output("Client {0} disconnected", endpoint));
                    runner.Run(() => RemoveClient(rt));
                    disposer.Clear();
                }
            });
        }

        private void RemoveClient(ClientRt rt)
        {
            register.Run(() =>
            {
                clients.Remove(rt);
                Tools.Try(rt.Dispose);
            });
        }

        public void Execute(Output output, params string[] tokens)
        {
            if (tokens[0] == "client")
            {
                var named = new NamedOutput("LISTENER", output);
                if (tokens.Length == 2 && tokens[1] == "list")
                {
                    register.Run(() =>
                    {
                        named.Output("Endpoint|Start");
                        foreach (var rt in clients)
                        {
                            named.Output("{0}|{1}", rt.EndPoint, Tools.Format(rt.Start));
                        }
                        named.Output("{0} client(s)", clients.Count);
                    }, named.OnException);
                }
            }
        }
    }

    public class ClientRt : Disposable
    {
        public Output Output;
        public TcpClient TcpClient;
        public IPEndPoint EndPoint;
        public WriterOutput Writer;
        public StreamReader Reader;
        public DateTime Start;
        public Runner Runner;
        public Shell Shell;

        protected override void Dispose(bool disposed)
        {
            Tools.Try(TcpClient.Close);
            Tools.Try(Runner.Dispose);
        }

        public void ReadLoop()
        {
            var line = Reader.ReadLine();
            while (line != null)
            {
                Output.Output("Client {0} < {1}", EndPoint, line);
                var tokens = Tools.Tokens(line, Writer);
                if (tokens != null && tokens.Length > 0) Shell.Execute(Writer, tokens);
                line = Reader.ReadLine();
            }
        }
    }
}