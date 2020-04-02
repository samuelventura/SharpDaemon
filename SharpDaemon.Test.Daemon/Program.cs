using System;

namespace SharpDaemon.Test.Daemon
{
    class CliArgs
    {
        public int DelayMs { get; set; }
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

            using (var runner = new Runner(new Runner.Args
            {
                IdleAction = Updater,
                IdleMsDelay = Math.Max(100, cargs.DelayMs),
                ExceptionHandler = Tools.ExceptionHandler,
            }))
            {
                var line = Console.ReadLine();
                while (line != null) line = Console.ReadLine();
            }

            Environment.Exit(0);
        }

        static void Updater()
        {
            Stdio.WriteLine("Ticks_{0}", DateTime.Now.Ticks);
        }
    }
}