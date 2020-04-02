using System;
using System.IO;

namespace SharpDaemon.Server
{
    public static class Launcher
    {
        public class CliArgs
        {
            public int Delay;
            public int Port;
            public string Ip;
            public string Ws;
        }

        public static CliArgs Default()
        {
            return new Launcher.CliArgs
            {
                Port = 22333,
                Delay = 5000,
                Ip = "0.0.0.0",
                Ws = Tools.Relative("Workspace"),
            };
        }

        public static CliArgs ParseCli(Output output, CliArgs cargs, params string[] args)
        {
            foreach (var arg in args)
            {
                output.WriteLine(arg);

                if (arg.StartsWith("delay="))
                {
                    cargs.Delay = int.Parse(arg.Split(new char[] { '=' }, 2)[1]);
                }
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
            var eppath = Path.Combine(args.Ws, "SharpDaemon.ep.txt");
            var downloads = Path.Combine(args.Ws, "Downloads");

            Directory.CreateDirectory(downloads);

            using (var disposer = new Disposer())
            {
                //acts like mutex for the workspace
                var writer = new StreamWriter(logpath, true);
                disposer.Push(writer);
                outputs.Add(new WriterOutput(writer));
                var instance = new Instance(new Instance.Args
                {
                    DbPath = dbpath,
                    RestartDelay = args.Delay,
                    Downloads = downloads,
                    Output = outputs,
                    TcpPort = args.Port,
                    IpAddress = args.Ip,
                });
                disposer.Push(instance);
                File.WriteAllText(eppath, instance.EndPoint.ToString());
                named.WriteLine("Listening on {0}", instance.EndPoint);
                disposer.Clear();
                return instance;
            }
        }
    }
}