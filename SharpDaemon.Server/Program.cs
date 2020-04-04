using System;

namespace SharpDaemon.Server
{
    public class Program
    {
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += Tools.ExceptionHandler;

            var outputs = new Outputs();
            outputs.Add(new ConsoleOutput());
            Disposable.Debug = outputs;

            var named = new NamedOutput("PROGRAM", outputs);

            var cargs = Launcher.Default();

            Launcher.ParseCli(outputs, cargs, args);

            using (var instance = Launcher.Launch(outputs, cargs))
            {
                named.WriteLine("ReadLine loop...");
                var shell = instance.CreateShell();
                var writer = new WriterOutput(Console.Out);
                var io = new StreamIO(writer, Console.In);
                var line = io.ReadLine();
                while (line != null)
                {
                    shell.ParseAndExecute(io, line);
                    line = io.ReadLine();
                }
                named.WriteLine("Stdin closed");
            }

            Environment.Exit(0);
        }
    }
}