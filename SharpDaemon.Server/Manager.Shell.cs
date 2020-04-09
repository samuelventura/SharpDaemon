using System;
using System.IO;
using System.Management;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace SharpDaemon.Server
{
    public partial class Manager : IShell
    {
        public void Execute(IStream io, params string[] tokens)
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
                if (tokens.Length == 3 && tokens[1] == "killall" && tokens[2] == "daemons")
                {
                    Execute(io, () => ExecuteKillAllDaemons(io, tokens));
                }
                if (tokens.Length == 3 && tokens[1] == "killall" && tokens[2] == "children")
                {
                    Execute(io, () => ExecuteKillAllChildren(io, tokens));
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
                io.WriteLine("daemon kill <optional-id>");
                io.WriteLine("daemon killall daemons");
                io.WriteLine("daemon killall children");
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
            AssertTools.True(dto != null, "Daemon {0} not found", id);
            Database.Remove(database, id);
            io.WriteLine("Daemon {0} uninstalled", id);
            ReloadDatabase();
        }

        private void ExecuteKill(IOutput io, params string[] tokens)
        {
            var id = tokens[2];
            running.TryGetValue(id, out var rt);
            AssertTools.True(rt != null, "Daemon {0} not found", id);
            Process.GetProcessById(rt.Pid).Kill();
            io.WriteLine("Daemon {0} killed", id);
        }

        private void ExecuteKillAllDaemons(IOutput io, params string[] tokens)
        {
            foreach (var rt in running.Values)
            {
                try
                {
                    Process.GetProcessById(rt.Pid).Kill();
                    io.WriteLine("Daemon {0} killed", rt.Id);
                }
                catch (Exception ex) { io.WriteLine("Error killing {0} {1} {2}", rt.Id, rt.Pid, ex); }
            }
            io.WriteLine("{0} total", running.Count);
        }

        private void ExecuteKillAllChildren(IOutput io, params string[] tokens)
        {
            var count = 0;
            var process = Process.GetCurrentProcess();
            io.WriteLine("Current process id {0}", process.Id);
            var mos = new ManagementObjectSearcher($"Select * From Win32_Process Where ParentProcessID={process.Id}");
            foreach (var mo in mos.Get())
            {
                count++;
                var pid = mo["ProcessID"].ToString();
                try
                {
                    Process.GetProcessById(int.Parse(pid)).Kill();
                    io.WriteLine("Child {0} killed", pid);
                }
                catch (Exception ex) { io.WriteLine("Error killing {0} {1}", pid, ex); }
            }
            io.WriteLine("{0} total", count);
        }

        private void ExecuteInstall(IOutput io, params string[] tokens)
        {
            var id = tokens[2];
            installed.TryGetValue(id, out var dto);
            AssertTools.True(dto == null, "Daemon {0} already installed", id);
            AssertTools.True(Regex.IsMatch(id, "[a-zA_Z][a-zA_Z0-9_]*"), "Invalid id {0}", id);
            var exe = tokens[3];
            AssertTools.True(PathTools.IsChildPath(root, exe), "Invalid path {0}", exe);
            dto = new DaemonDto
            {
                Id = id,
                Path = tokens[3],
                Args = DaemonProcess.MakeCli(tokens, 4),
            };
            var path = PathTools.Combine(root, dto.Path);
            AssertTools.True(File.Exists(path), "File {0} not found", dto.Path);
            Database.Save(database, dto);
            io.WriteLine("Daemon {0} installed as {1}", id, dto.Info("Path|Args"));
            ReloadDatabase();
        }

        private void Execute(IOutput io, Action action) => runner.Run(action, io.HandleException);
    }
}