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
            var workspace = null as string;

            foreach (var arg in args)
            {
                named.Output(arg);

                if (arg.StartsWith("port="))
                {
                    port = int.Parse(arg.Split(new char[] { '=' }, 2)[1]);
                }
                if (arg.StartsWith("workspace="))
                {
                    workspace = arg.Split(new char[] { '=' }, 2)[1].Replace("[20]", " ");
                }
            }

            using (var instance = Launch(outputs, port, workspace))
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

        public static Instance Launch(Outputs outputs, int port = 0, string workspace = null)
        {
            var named = new NamedOutput("LAUNCHER", outputs);

            workspace = workspace ?? Tools.Relative("Workspace");

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
                });
                disposer.Push(instance);
                named.Output("Listening at {0}", instance.Port);
                File.WriteAllText(portpath, instance.Port.ToString());
                disposer.Clear();
                return instance;
            }
        }
    }
}