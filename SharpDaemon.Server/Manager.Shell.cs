using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace SharpDaemon.Server
{
    public partial class Manager : IShell
    {
        public void Execute(Shell.IO io, params string[] tokens)
        {
            if (tokens[0] == "daemon")
            {
                if (tokens.Length == 3 && tokens[1] == "list" && tokens[2] == "installed")
                {
                    Execute(io, () => ExecuteListInstalled(io, tokens));
                }
                if (tokens.Length == 3 && tokens[1] == "list" && tokens[2] == "running")
                {
                    Execute(io, () => ExecuteListRunning(io, tokens));
                }
                if (tokens.Length == 3 && tokens[1] == "uninstall")
                {
                    Execute(io, () => ExecuteUninstall(io, tokens));
                }
                if (tokens.Length == 3 && tokens[1] == "kill")
                {
                    Execute(io, () => ExecuteKill(io, tokens));
                }
                if (tokens.Length >= 4 && tokens[1] == "install")
                {
                    Execute(io, () => ExecuteInstall(io, tokens));
                }
            }
            if (tokens[0] == "help")
            {
                io.WriteLine("daemon list installed");
                io.WriteLine("daemon list running");
                io.WriteLine("daemon install <id> <exe-relative-path> <optional-list-of-args>");
                io.WriteLine(" sample: daemon install sample1 sample1/main.exe 1 2 3 `a b c`");
                io.WriteLine("daemon uninstall <id>");
                io.WriteLine("daemon kill <id>");
            }
        }

        private void ExecuteListInstalled(IOutput io, params string[] tokens)
        {
            io.WriteLine("Id|Path|Args");
            foreach (var dto in installed.Values)
            {
                io.WriteLine(dto.Info("Id|Path|Args"));
            }
            io.WriteLine("{0} daemon(s) installed", installed.Count);
        }

        private void ExecuteListRunning(IOutput io, params string[] tokens)
        {
            io.WriteLine("Id|Name|Pid|Status");
            foreach (var rt in running.Values)
            {
                io.WriteLine(rt.Info("Id|Name|Pid|Status"));
            }
            io.WriteLine("{0} daemon(s) running", running.Count);
        }

        private void ExecuteUninstall(IOutput io, params string[] tokens)
        {
            var id = tokens[2];
            installed.TryGetValue(id, out var dto);
            Tools.Assert(dto != null, "Daemon {0} not found", id);
            Database.Remove(database, id);
            io.WriteLine("Daemon {0} uninstalled", id);
            ReloadDatabase();
        }

        private void ExecuteKill(IOutput io, params string[] tokens)
        {
            var id = tokens[2];
            running.TryGetValue(id, out var rt);
            Tools.Assert(rt != null, "Daemon {0} not found", id);
            Process.GetProcessById(rt.Pid).Kill();
            io.WriteLine("Daemon {0} killed", id);
        }

        private void ExecuteInstall(IOutput io, params string[] tokens)
        {
            var id = tokens[2];
            installed.TryGetValue(id, out var dto);
            Tools.Assert(dto == null, "Daemon {0} already installed", id);
            Tools.Assert(Regex.IsMatch(id, "[a-zA_Z][a-zA_Z0-9_]*"), "Invalid id {0}", id);
            var exe = tokens[3];
            Tools.Assert(Tools.IsChildPath(root, exe), "Invalid path {0}", exe);
            var args = new StringBuilder();
            for (var i = 0; i < tokens.Length - 4; i++)
            {
                //single point of control for offset setup it in tokens.Length - 4
                var arg = tokens[i + 4];
                if (args.Length > 0) args.Append(" ");
                Tools.Assert(!arg.Contains("\""), "Invalid arg {0} {1}", i, arg);
                if (arg.Contains(" ")) args.Append("\"");
                args.Append(arg);
                if (arg.Contains(" ")) args.Append("\"");
            }
            dto = new DaemonDto
            {
                Id = id,
                Path = tokens[3],
                Args = args.ToString(),
            };
            var path = Tools.Combine(root, dto.Path);
            Tools.Assert(File.Exists(path), "File {0} not found", dto.Path);
            Database.Save(database, dto);
            io.WriteLine("Daemon {0} installed as {1}", id, dto.Info("Path|Args"));
            ReloadDatabase();
        }

        private void Execute(IOutput io, Action action) => runner.Run(action, io.OnException);
    }
}