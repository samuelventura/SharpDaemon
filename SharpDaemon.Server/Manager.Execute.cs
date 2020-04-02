using System;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.IO.Compression;

namespace SharpDaemon.Server
{
    public partial class Manager : IScriptable
    {
        public void Execute(Output output, params string[] tokens)
        {
            if (tokens[0] == "daemon")
            {
                var named = new NamedOutput("MANAGER", output);

                if (tokens.Length == 3 && tokens[1] == "list" && tokens[2] == "installed")
                {
                    Execute(named, () => ExecuteListInstalled(named, tokens));
                }
                if (tokens.Length == 3 && tokens[1] == "list" && tokens[2] == "running")
                {
                    Execute(named, () => ExecuteListRunning(named, tokens));
                }
                if (tokens.Length == 3 && tokens[1] == "uninstall")
                {
                    Execute(named, () => ExecuteUninstall(named, tokens));
                }
                if (tokens.Length == 3 && tokens[1] == "kill")
                {
                    Execute(named, () => ExecuteKill(named, tokens));
                }
                if (tokens.Length == 3 && tokens[1] == "install")
                {
                    Execute(named, () => ExecuteInstall3(named, tokens));
                }
                if ((tokens.Length == 4 || tokens.Length == 5) && tokens[1] == "install")
                {
                    Execute(named, () => ExecuteInstall45(named, tokens));
                }
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
            if (rt != null) Process.GetProcessById(rt.Pid).Kill();
        }

        private void ExecuteInstall45(Output output, params string[] tokens)
        {
            var dto = new DaemonDto
            {
                Id = tokens[2],
                Path = tokens[3],
                Args = tokens.Length > 4 ? tokens[4] : string.Empty,
            };
            output.WriteLine("Daemon {0} installing {1}...", dto.Id, dto.Info("Path|Args"));
            Database.Save(database, dto);
            output.WriteLine("Daemon {0} installed", dto.Id);
            ReloadDatabase();
        }

        private void ExecuteInstall3(Output output, params string[] tokens)
        {
            if (Uri.TryCreate(tokens[2], UriKind.Absolute, out var uri))
            {
                var zipfile = Path.GetFileName(uri.LocalPath);
                var zipfilepath = Path.Combine(downloads, zipfile);
                var zipdir = zipfile.Replace(".", "_");
                var zipdirpath = Path.Combine(downloads, zipdir);
                if (Path.GetExtension(zipfile) == ".zip")
                {
                    output.WriteLine("Downloading {0}...", uri);
                    using (var client = new WebClient()) client.DownloadFile(uri, zipfilepath);
                    Directory.CreateDirectory(zipdirpath);
                    ZipFile.ExtractToDirectory(zipfilepath, zipdirpath);
                    var lines = File.ReadAllText(Path.Combine(zipdirpath, "Main.txt")).Split(new char[] { '\n' }, 2);
                    var exefile = lines[0].Trim(); //executable in first line
                    var exeargs = string.Empty; //args in all the others
                    if (lines.Length > 1) exeargs = lines[1].Replace('\n', ' ').Trim();
                    var exepath = Path.Combine(zipdir, exefile); //relative
                    ExecuteInstall45(output, "daemon", "install", zipfile, exepath, exeargs);
                }
            }
        }

        private void Execute(NamedOutput named, Action action) => runner.Run(action, named.OnException);
    }
}