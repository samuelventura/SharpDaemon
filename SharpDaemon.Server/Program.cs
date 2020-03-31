﻿using System;
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

            var workspace = null as string;

            foreach (var arg in args)
            {
                stdout.Output(arg);
                if (arg.StartsWith("ws="))
                {
                    workspace = arg.Split(new char[] { '=' }, 2)[1].Replace("[20]", " ");
                }
            }

            using (var instance = Launch(outputs, workspace))
            {
                stdout.Output("ReadLine loop...");
                var shell = instance.CreateShell();
                var line = Console.ReadLine();
                while (line != null)
                {
                    shell.OnLine(line, stdout);
                    line = Console.ReadLine();
                }
                stdout.Output("Stdin closed");
            }

            Environment.Exit(0);
        }

        public static Instance Launch(Outputs outputs, string workspace = null)
        {

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
                });
                disposer.Push(instance);
                outputs.Output("Listening at {0}", instance.Port);
                File.WriteAllText(portpath, instance.Port.ToString());
                disposer.Clear();
                return instance;
            }
        }
    }
}