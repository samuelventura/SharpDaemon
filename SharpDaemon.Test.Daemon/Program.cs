using System;
using System.IO;
using System.Threading;
using SharpDaemon;

namespace SharpDaemon.Test.Daemon
{
    class CliArgs
    {
        public string Mode { get; set; }
        public string Data { get; set; }
        public int Delay { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += Tools.ExceptionHandler;

            var cargs = new CliArgs();

            foreach (var arg in args)
            {
                Tools.SetProperty(cargs, arg);
            }

            Process(cargs);

            var line = Console.ReadLine();
            while (line != null) line = Console.ReadLine();

            Environment.Exit(0);
        }

        static void Process(CliArgs cargs)
        {
            switch (cargs.Mode)
            {
                case "Echo":
                    Thread.Sleep(cargs.Delay);
                    Stdio.WriteLine(cargs.Data);
                    Thread.Sleep(cargs.Delay);
                    Environment.Exit(0);
                    break;
            }
        }
    }
}