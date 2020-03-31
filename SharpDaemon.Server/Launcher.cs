using System;
using System.IO;

namespace SharpDaemon.Server
{
    public static class Launcher
    {
        public class CliArgs
        {
            public int Port;
            public string Ip;
            public string Ws;
        }

        public static CliArgs ParseCli(Output output, string[] args)
        {
            var cargs = new CliArgs
            {
                Port = 0,
                Ip = "127.0.0.1",
                Ws = Tools.Relative("Workspace"),
            };

            foreach (var arg in args)
            {
                output.Output(arg);

                if (arg.StartsWith("port="))
                {
                    cargs.Port = int.Parse(arg.Split(new char[] { '=' }, 2)[1]);
                }
                if (arg.StartsWith("ws="))
                {
                    cargs.Ws = arg.Split(new char[] { '=' }, 2)[1].Replace("[20]", " ");
                }
                if (arg.StartsWith("ip="))
                {
                    cargs.Ip = arg.Split(new char[] { '=' }, 2)[1];
                }
            }

            return cargs;
        }

        public static Instance Launch(Outputs outputs, CliArgs args)
        {
            var named = new NamedOutput("LAUNCHER", outputs);

            var dbpath = Path.Combine(args.Ws, "SharpDaemon.litedb");
            var logpath = Path.Combine(args.Ws, "SharpDaemon.log.txt");
            var portpath = Path.Combine(args.Ws, "SharpDaemon.ep.txt");
            var downloads = Path.Combine(args.Ws, "Downloads");

            Directory.CreateDirectory(downloads);

            using (var disposer = new Disposer())
            {
                //acts like mutex for the workspace
                var writer = new StreamWriter(logpath, true);
                disposer.Push(writer);
                var fileout = new WriterOutput(writer);
                outputs.Add(fileout);
                var instance = new Instance(new Instance.Args
                {
                    DbPath = dbpath,
                    RestartDelay = 2000,
                    Downloads = downloads,
                    Outputs = outputs,
                    TcpPort = args.Port,
                    IpAddress = args.Ip,
                });
                disposer.Push(instance);
                named.Output("Listening on {0}", instance.EndPoint);
                File.WriteAllText(portpath, instance.EndPoint.ToString());
                disposer.Clear();
                return instance;
            }
        }
    }
}