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
            }
        }

        private void ExecuteRun(IStream stream, params string[] tokens)
        {
            var exe = tokens[1];
            var args = new StringBuilder();
            for (var i = 0; i < tokens.Length - 2; i++)
            {
                //single point of control for offset setup it in tokens.Length - 4
                var arg = tokens[i];
                if (args.Length > 0) args.Append(" ");
                AssertTools.True(!arg.Contains("\""), "Invalid arg {0} {1}", i, arg);
                if (arg.Contains(" ")) args.Append("\"");
                args.Append(arg);
                if (arg.Contains(" ")) args.Append("\"");
            }
            DaemonProcess.Interactive(stream, new DaemonProcess.Args
            {
                Executable = exe.Replace("{root}", downloads),
                Arguments = args.ToString().Replace("{root}", downloads),
            });
        }
    }
}