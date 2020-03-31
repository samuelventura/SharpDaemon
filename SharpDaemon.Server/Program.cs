using System;
using System.IO;

namespace SharpDaemon.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += Tools.ExceptionHandler;

            var stdout = new StdOutput();
            var outputs = new Outputs();
            outputs.Add(stdout);

            var named = new NamedOutput("PROGRAM", outputs);

            var port = 0;
            var ip = "0.0.0.0";
            var ws = Tools.Relative("Workspace");

            foreach (var arg in args)
            {
                named.Output(arg);

                if (arg.StartsWith("port="))
                {
                    port = int.Parse(arg.Split(new char[] { '=' }, 2)[1]);
                }
                if (arg.StartsWith("ws="))
                {
                    ws = arg.Split(new char[] { '=' }, 2)[1].Replace("[20]", " ");
                }
                if (arg.StartsWith("ip="))
                {
                    ip = arg.Split(new char[] { '=' }, 2)[1];
                }
            }

            using (var instance = Launch(outputs, ip, port, ws))
            {
                named.Output("ReadLine loop...");
                var shell = instance.CreateShell();
                var line = Console.ReadLine();
                while (line != null)
                {
                    var tokens = Tools.Tokens(line, stdout);
                    if (tokens != null && tokens.Length > 0) shell.OnLine(tokens, stdout);
                    line = Console.ReadLine();
                }
                named.Output("Stdin closed");
            }

            Environment.Exit(0);
        }

        public static Instance Launch(Outputs outputs, string ip, int port, string workspace)
        {
            var named = new NamedOutput("LAUNCHER", outputs);

            var dbpath = Path.Combine(workspace, "daemons.db");
            var logpath = Path.Combine(workspace, "daemons.txt");
            var portpath = Path.Combine(workspace, "port.txt");
            var downloads = Path.Combine(workspace, "Downloads");

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
                    TcpPort = port,
                    IpAddress = ip,
                });
                disposer.Push(instance);
                named.Output("Listening on {0}", instance.EndPoint);
                File.WriteAllText(portpath, instance.EndPoint.Port.ToString());
                disposer.Clear();
                return instance;
            }
        }
    }
}