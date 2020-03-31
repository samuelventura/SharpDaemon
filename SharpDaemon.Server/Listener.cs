﻿using System;
using System.IO;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Collections.Generic;

namespace SharpDaemon.Server
{
    public class Listener : IDisposable, IScriptable
    {
        private readonly ShellFactory factory;
        private readonly HashSet<ClientRt> clients;
        private readonly Action<Exception> handler;
        private readonly TcpListener server;
        private readonly Runner accepter;
        private readonly Runner register;
        private readonly Output output;
        private readonly int port;

        public class Args
        {
            public int TcpPort { get; set; }
            public Output Output { get; set; }
            public ShellFactory ShellFactory { get; set; }
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
                //accepter wont exit otherwise
                disposer.Push(server.Stop);
                disposer.Clear();
            }
        }

        public void Start()
        {
            accepter.Run(AcceptLoop);
        }

        public int Port { get { return port; } }

        public void Dispose()
        {
            Tools.Try(server.Stop, handler);
            Tools.Try(accepter.Dispose, handler);
            Tools.Try(register.Dispose, handler);
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
                    var stream = client.GetStream();
                    var endpoint = client.Client.RemoteEndPoint as IPEndPoint;
                    output.Output("Client connected from {0}", endpoint);
                    var runner = new Runner(new Runner.Args { ExceptionHandler = handler });
                    disposer.Push(runner);
                    var rt = new ClientRt
                    {
                        Output = output,
                        Runner = runner,
                        EndPoint = endpoint,
                        TcpClient = client,
                        Start = DateTime.Now,
                        Remove = RemoveClient,
                        Shell = factory.Create(),
                        Reader = new StreamReader(stream, Encoding.UTF8),
                        Writer = new WriterOutput(new StreamWriter(stream, Encoding.UTF8)),
                    };
                    clients.Add(rt);
                    runner.Run(rt.Loop);
                    output.Output("Client {0} added", endpoint);
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

        public void Execute(string[] tokens, Output output)
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
                            var start = rt.Start.ToString("yyyy-MM-dd HH:mm:ss.fff");
                            named.Output("{0}|{1}", rt.EndPoint, start);
                        }
                        named.Output("{0} client(s)", clients.Count);
                    }, named.OnException);
                }
            }
        }
    }

    public class ClientRt : IDisposable
    {
        public Output Output;
        public Action<ClientRt> Remove;
        public Action<Exception> Handler;
        public TcpClient TcpClient;
        public IPEndPoint EndPoint;
        public WriterOutput Writer;
        public StreamReader Reader;
        public DateTime Start;
        public Runner Runner;
        public Shell Shell;

        public void Dispose()
        {
            Tools.Try(TcpClient.Close, Handler);
            Tools.Try(Runner.Dispose, Handler);
        }

        public void Loop()
        {
            Tools.Try(Process, Handler);
            Output.Output("Client {0} disconnected", EndPoint);
            Remove(this);
        }

        public void Process()
        {
            var line = Reader.ReadLine();
            while (line != null)
            {
                Output.Output("{0} < {1}", EndPoint, line);
                var tokens = Tools.Tokens(line, Writer);
                if (tokens != null && tokens.Length > 0) Shell.OnLine(tokens, Writer);
                line = Reader.ReadLine();
            }
        }
    }
}