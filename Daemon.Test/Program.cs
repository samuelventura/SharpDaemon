using System;
using SharpDaemon;

namespace Daemon.Test
{
    class Program
    {
        class Config
        {
            public string Id { get; set; }
            public bool Daemon { get; set; }
        }

        static void Main(string[] args)
        {
            ProgramTools.Setup();

            var config = new Config();

            ExecutableTools.LogArgs(new StderrWriteLine(), args, (arg) =>
            {
                ConfigTools.SetProperty(config, arg);
            });

            if (!config.Daemon) Logger.TRACE = new StderrWriteLine();

            Stdio.SetStatus("Ready");

            var line = Stdio.ReadLine();

            while (line != null)
            {
                Logger.Trace("> {0}", line);
                if (line == "ping")
                {
                    Stdio.WriteLine("pong");
                    Logger.Trace("< pong");
                }
                //throw Exception Message
                if (line.StartsWith("throw")) throw new Exception(line.Substring(6));
                line = Stdio.ReadLine();
            }

            Logger.Trace("Stdin closed");

            Environment.Exit(0);
        }
    }
}