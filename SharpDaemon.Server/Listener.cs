using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace SharpDaemon.Server
{
    public partial class Listener : Disposable
    {
        private readonly Dictionary<string, ClientRt> clients;
        private readonly X509Certificate2 certificate;
        private readonly ShellFactory factory;
        private readonly IOutput output;
        private readonly IOutput named;
        private readonly IPEndPoint endpoint;
        private readonly TcpListener server;
        private readonly Runner accepter;
        private readonly Runner register;

        public class Args
        {
            public IOutput Output { get; set; }
            public IPEndPoint EndPoint { get; set; }
            public ShellFactory ShellFactory { get; set; }
        }

        public IPEndPoint EndPoint { get { return endpoint; } }

        public Listener(Args args)
        {
            var certfile = ExecutableTools.Relative("DaemonManager.pfx");
            certificate = new X509Certificate2(certfile, "none");
            factory = args.ShellFactory;
            output = args.Output;
            clients = new Dictionary<string, ClientRt>();
            named = new NamedOutput(args.Output, "LISTENER");
            server = new TcpListener(args.EndPoint);
            server.MakeNotInheritable();
            using (var disposer = new Disposer())
            {
                //push must match dispose order
                register = new Runner(new Runner.Args
                {
                    ExceptionHandler = named.HandleException,
                    ThreadName = "Register",
                });
                disposer.Push(register);
                accepter = new Runner(new Runner.Args
                {
                    ExceptionHandler = named.HandleException,
                    ThreadName = "Accepter",
                });
                disposer.Push(accepter);

                disposer.Push(Dispose); //ensure cleanup order
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
            //https://github.com/dotnet/runtime/issues/24513 fixed in net5
            //netcore macos hangs on stop even with previous shutdown
            //netcore linux requires shutdown 
            ExceptionTools.Try(() => server.Server.Shutdown(SocketShutdown.Both));
            //net462 works ok just with stop but requires MakeNotInheritable above
            ExceptionTools.Try(server.Stop);
            accepter.Dispose();
            register.Dispose(() =>
            {
                foreach (var rt in clients.Values) rt.Dispose();
                clients.Clear();
            });
        }

        private void AcceptLoop()
        {
            while (true)
            {
                var client = server.AcceptTcpClient();
                register.Run(() =>
                {
                    using (var disposer = new Disposer())
                    {
                        disposer.Push(client);
                        var rt = new ClientRt(client, certificate, output, factory);
                        disposer.Push(rt.Dispose); //ensure cleanup order
                        clients.Add(rt.EndPoint.ToString(), rt);
                        rt.Run(() => RemoveClient(rt));
                        named.WriteLine("Client {0} connected", rt.EndPoint);
                        disposer.Clear();
                    }
                });
            }
        }

        private void RemoveClient(ClientRt rt)
        {
            register.Run(() =>
            {
                named.WriteLine("Client {0} disconnected", rt.EndPoint);
                clients.Remove(rt.EndPoint.ToString());
                rt.Dispose();
            });
        }
    }
}