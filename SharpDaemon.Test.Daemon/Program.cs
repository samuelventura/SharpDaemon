using System;
using System.IO;
using SharpDaemon;

namespace SharpDaemon.Test.Daemon
{
    public class Program
    {
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += Tools.ExceptionHandler;

            foreach (var arg in args)
            {
                Logger.Debug("Arg {0}", arg);
            }

            var cmd = string.Join(" ", args);

            if (cmd.StartsWith("echo"))
            {
                Logger.Info(cmd.Substring("echo".Length).Trim());
            }

            Environment.Exit(0);
        }
    }
}