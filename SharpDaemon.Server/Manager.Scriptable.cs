using System;
using System.Diagnostics;

namespace SharpDaemon.Server
{
    public partial class Manager : IScriptable
    {
        public void Execute(Output output, params string[] tokens)
        {
            if (tokens[0] == "daemon")
            {
                if (tokens.Length == 3 && tokens[1] == "list" && tokens[2] == "installed")
                {
                    Execute(output, () => ExecuteListInstalled(output, tokens));
                }
                if (tokens.Length == 3 && tokens[1] == "list" && tokens[2] == "running")
                {
                    Execute(output, () => ExecuteListRunning(output, tokens));
                }
                if (tokens.Length == 3 && tokens[1] == "uninstall")
                {
                    Execute(output, () => ExecuteUninstall(output, tokens));
                }
                if (tokens.Length == 3 && tokens[1] == "kill")
                {
                    Execute(output, () => ExecuteKill(output, tokens));
                }
                if (tokens.Length == 4 && tokens[1] == "install")
                {
                    Execute(output, () => ExecuteInstall(output, tokens));
                }
                if (tokens.Length == 5 && tokens[1] == "install")
                {
                    Execute(output, () => ExecuteInstall(output, tokens));
                }
            }
            if (tokens[0] == "help")
            {
                output.WriteLine("daemon list installed");
                output.WriteLine("daemon list running");
                output.WriteLine("daemon install <id> <exe-path> <optiona-args>");
                output.WriteLine(" sample : daemon install adder sample/add.exe `1 2 3`");
                output.WriteLine("daemon uninstall <id>");
                output.WriteLine("daemon kill <id>");
            }
        }

        private void ExecuteListInstalled(Output output, params string[] tokens)
        {
            output.WriteLine("Id|Path|Args");
            foreach (var dto in installed.Values)
            {
                output.WriteLine(dto.Info("Id|Path|Args"));
            }
            output.WriteLine("{0} daemon(s) installed", installed.Count);
        }

        private void ExecuteListRunning(Output output, params string[] tokens)
        {
            output.WriteLine("Id|Name|Pid|Status");
            foreach (var rt in running.Values)
            {
                output.WriteLine(rt.Info("Id|Name|Pid|Status"));
            }
            output.WriteLine("{0} daemon(s) running", running.Count);
        }

        private void ExecuteUninstall(Output output, params string[] tokens)
        {
            var id = tokens[2];
            output.WriteLine("Daemon {0} uninstalling...", id);
            Database.Remove(database, id);
            output.WriteLine("Daemon {0} uninstalled", id);
            ReloadDatabase();
        }

        private void ExecuteKill(Output output, params string[] tokens)
        {
            var id = tokens[2];
            running.TryGetValue(id, out var rt);
            if (rt != null)
            {
                Process.GetProcessById(rt.Pid).Kill();
                output.WriteLine("Daemon {0} killed", id);
            }
        }

        private void ExecuteInstall(Output output, params string[] tokens)
        {
            var dto = new DaemonDto
            {
                Id = tokens[2],
                Path = tokens[3],
                Args = tokens.Length == 4 ? string.Empty : tokens[4],
            };
            output.WriteLine("Daemon {0} installing {1}...", dto.Id, dto.Info("Path|Args"));
            Database.Save(database, dto);
            output.WriteLine("Daemon {0} installed", dto.Id);
            ReloadDatabase();
        }

        private void Execute(Output output, Action action) => runner.Run(action, output.OnException);
    }
}