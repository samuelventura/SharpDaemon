using System;

namespace SharpDaemon.Server
{
    public class Program
    {
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += Tools.ExceptionHandler;

            var stdout = new StdOutput();
            var outputs = new Outputs();
            outputs.Add(stdout);

            var named = new NamedOutput("PROGRAM", outputs);

            var cargs = Launcher.ParseCli(outputs, args);

            using (var instance = Launcher.Launch(outputs, cargs))
            {
                named.Output("ReadLine loop...");
                var shell = instance.CreateShell();
                var line = Console.ReadLine();
                while (line != null)
                {
                    var tokens = Tools.Tokens(line, stdout);
                    if (tokens != null && tokens.Length > 0) shell.Execute(tokens, stdout);
                    line = Console.ReadLine();
                }
                named.Output("Stdin closed");
            }

            Environment.Exit(0);
        }
    }
}