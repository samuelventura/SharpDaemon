using System;
using System.IO;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Collections.Generic;

namespace SharpDaemon.Server
{
    public interface Shell : IDisposable
    {
        void OnLine(string line, Action<string> output);
    }

    public interface ShellFactory : IDisposable
    {
        Shell Create();
    }

    public class Listener : IDisposable
    {
        private readonly ShellFactory factory;
        private readonly HashSet<ClientRt> clients;
        private readonly Action<Exception> handler;
        private readonly Action<String> output;
        private readonly TcpListener server;
        private readonly Runner accepter;
        private readonly Runner register;
        private readonly int port;

        public class Args
        {
            public int TcpPort { get; set; }
            public ShellFactory ShellFactory { get; set; }
            public Action<String> Output { get; set; }
            public Action<Exception> ExceptionHandler { get; set; }
        }

        public Listener(Args args = null)
        {
            args = args ?? new Args();
            port = args.TcpPort;
            output = args.Output;
            factory = args.ShellFactory;
            handler = args.ExceptionHandler;
            clients = new HashSet<ClientRt>();
            server = new TcpListener(IPAddress.Any, port);
            using (var disposer = new Disposer(handler))
            {
                disposer.Push(server.Stop);
                server.Start();
                port = ((IPEndPoint)server.LocalEndpoint).Port;
                register = new Runner(new Runner.Args { ExceptionHandler = handler });
                disposer.Push(register);
                accepter = new Runner(new Runner.Args { ExceptionHandler = handler });
                disposer.Push(accepter);
                accepter.Run(AcceptLoop);
                //accepter wont exit otherwise
                disposer.Push(server.Stop);
                disposer.Clear();
            }
        }

        public int Port { get { return port; } }

        public void Dispose()
        {
            Tools.Try(server.Stop, handler);
            Tools.Try(accepter.Dispose, handler);
            Tools.Try(register.Dispose, handler);
            Tools.Try(factory.Dispose, handler);
            foreach (var rt in clients) Tools.Try(rt.Dispose, handler);
            clients.Clear();
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
                    var shell = factory.Create();
                    disposer.Push(shell);
                    var runner = new Runner(new Runner.Args { ExceptionHandler = handler });
                    disposer.Push(runner);
                    var rt = new ClientRt
                    {
                        Shell = shell,
                        Output = output,
                        TcpClient = client,
                        Remove = RemoveClient,
                        Stream = client.GetStream(),
                        EndPoint = client.Client.RemoteEndPoint as IPEndPoint,
                        Runner = runner,
                    };
                    clients.Add(rt);
                    Log("Client connected from {0}", rt.EndPoint);
                    rt.Runner.Run(rt.Loop);
                    disposer.Clear();
                }
            });
        }

        private void RemoveClient(ClientRt rt)
        {
            register.Run(() =>
            {
                clients.Remove(rt);
                Tools.Try(rt.Dispose, handler);
            });
        }

        private void Log(string format, params object[] args)
        {
            if (output != null)
            {
                var text = format;
                if (args.Length > 0) text = string.Format(format, args);
                output(text);
            }
        }
    }

    public class ClientRt : IDisposable
    {
        public Action<string> Output;
        public Action<ClientRt> Remove;
        public Action<Exception> Handler;
        public TcpClient TcpClient;
        public IPEndPoint EndPoint;
        public NetworkStream Stream;
        public Runner Runner;
        public Shell Shell;

        public void Dispose()
        {
            Tools.Try(Shell.Dispose, Handler);
            Tools.Try(TcpClient.Close, Handler);
            Tools.Try(Runner.Dispose, Handler);
        }

        public void Loop()
        {
            Tools.Try(Process, Handler);
            Log("Client {0} disconnected", EndPoint);
            Remove(this);
        }

        public void Process()
        {
            var writer = new StreamWriter(Stream, Encoding.UTF8);
            var reader = new StreamReader(Stream, Encoding.UTF8);
            var line = reader.ReadLine();
            while (line != null)
            {
                Log("{0} < {1}", EndPoint, line);
                Shell.OnLine(line, writer.WriteLine);
                line = reader.ReadLine();
            }
        }

        private void Log(string format, params object[] args)
        {
            if (Output != null)
            {
                var text = format;
                if (args.Length > 0) text = string.Format(format, args);
                Output(text);
            }
        }
    }
}