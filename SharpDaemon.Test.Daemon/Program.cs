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
                Logger.Info("Arg {0}", arg);
            }
        }
    }
}