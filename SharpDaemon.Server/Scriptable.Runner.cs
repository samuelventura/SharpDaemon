using System;
using System.IO;

namespace SharpDaemon.Server
{
    public class RunnerScriptable : IShell
    {
        private readonly string downloads;

        public RunnerScriptable(string downloads)
        {
            this.downloads = Path.GetFullPath(downloads); //canonic
        }

        public void Execute(IStream stream, params string[] tokens)
        {
            if (tokens[0] == "run")
            {
                if (tokens.Length >= 2)
                {
                    ExecuteRun(stream, tokens);
                }
            }
            if (tokens[0] == "help")
            {
                stream.WriteLine("run <absolute-exe-path> <optional-argument-list>");
                stream.WriteLine(" {root} is replaced with downloads root path");
            }
        }

        private void ExecuteRun(IStream stream, params string[] tokens)
        {
            var exe = tokens[1];
            var args = DaemonProcess.MakeCli(tokens, 2);
            DaemonProcess.Interactive(stream, new DaemonProcess.Args
            {
                Executable = exe.Replace("{root}", downloads),
                Arguments = args.ToString().Replace("{root}", downloads),
            });
        }
    }
}