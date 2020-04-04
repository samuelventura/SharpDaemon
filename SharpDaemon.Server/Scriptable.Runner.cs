using System;
using System.IO;
using System.Text;

namespace SharpDaemon.Server
{
    public class RunnerScriptable : IShell
    {
        private readonly string downloads;

        public RunnerScriptable(string downloads)
        {
            this.downloads = Path.GetFullPath(downloads); //canonic
        }

        public void Execute(Shell.IO io, params string[] tokens)
        {
            if (tokens[0] == "run")
            {
                if (tokens.Length >= 2)
                {
                    ExecuteRun(io, tokens);
                }
            }
            if (tokens[0] == "help")
            {
                io.WriteLine("run <absolute-exe-path> <optional-argument-list>");
            }
        }

        private void ExecuteRun(Shell.IO io, params string[] tokens)
        {
            var exe = tokens[1];
            var args = new StringBuilder();
            for (var i = 0; i < tokens.Length - 2; i++)
            {
                //single point of control for offset setup it in tokens.Length - 4
                var arg = tokens[i];
                if (args.Length > 0) args.Append(" ");
                Tools.Assert(!arg.Contains("\""), "Invalid arg {0} {1}", i, arg);
                if (arg.Contains(" ")) args.Append("\"");
                args.Append(arg);
                if (arg.Contains(" ")) args.Append("\"");
            }
            DaemonProcess.Interactive(io, new DaemonProcess.Args
            {
                Executable = exe.Replace("{root}", downloads),
                Arguments = args.ToString().Replace("{root}", downloads),
            });
        }
    }
}