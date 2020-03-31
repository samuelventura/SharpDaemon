using System;
using System.IO;

namespace SharpDaemon.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += Tools.ExceptionHandler;

            var workspace = Tools.Relative("Workspace");

            foreach (var arg in args)
            {
                if (arg.StartsWith("ws="))
                {
                    workspace = ParsePath(arg);
                }
            }

            var dbpath = Path.Combine(workspace, "daemons.db");
            var logpath = Path.Combine(workspace, "daemons.txt");
            var portpath = Path.Combine(workspace, "port.txt");
            var downloads = Path.Combine(workspace, "Downloads");
            Directory.CreateDirectory(downloads);

            using (var instance = new Instance(new Instance.Args
            {
                Stdout = true,
                DbPath = dbpath,
                LogPath = logpath,
                RestartDelay = 2000,
                Downloads = downloads,
            }))
            {
                instance.Log("Listening at {0}", instance.Port);
                File.WriteAllText(portpath, instance.Port.ToString());
                instance.Log("ReadLine loop...");
                var line = Console.ReadLine();
                while (line != null)
                {
                    line = Console.ReadLine();
                }
                instance.Log("Stdin closed");
            }

            Environment.Exit(0);
        }

        static string ParsePath(string arg)
        {
            return arg.Split(new char[] { '=' }, 2)[1].Replace("[20]", " ");
        }
    }
}