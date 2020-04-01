using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;

namespace SharpDaemon.Server
{
    public partial class Listener : Disposable
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
            using (var disposer = new Disposer())
            {
                //push must match dispose order
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
                disposer.Push(server.Stop);
                server.Start();
                endpoint = server.LocalEndpoint as IPEndPoint;
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
                using (var disposer = new Disposer())
                {
                    disposer.Push(client);
                    var rt = new ClientRt(client, output, factory, handler);
                    disposer.Push(rt); //ensure cleanup order
                    clients.Add(rt);
                    rt.Run(() => RemoveClient(rt));
                    output.Output("Client {0} connected", rt.EndPoint);
                    disposer.Clear();
                }
            });
        }

        private void RemoveClient(ClientRt rt)
        {
            register.Run(() =>
            {
                output.Output("Client {0} disconnected", rt.EndPoint);
                clients.Remove(rt);
                Tools.Try(rt.Dispose);
            });
        }
    }
}