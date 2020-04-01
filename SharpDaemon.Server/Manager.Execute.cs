using System;
using System.IO;
using System.Net;
using System.IO.Compression;

namespace SharpDaemon.Server
{
    public partial class Manager : IScriptable
    {
        private void ExecuteStart(Output output, NamedOutput named, params string[] tokens)
        {
            named.Output("Loading daemons...");
            var dtos = Database.List(dbpath);
            foreach (var dto in dtos)
            {
                named.Output("Loading daemon {0}|{1}|{2}", dto.Id, dto.Path, dto.Args);
                controller.Execute(output, "daemon", "install", dto.Id, dto.Path, dto.Args);
            }
            named.Output("{0} daemon(s) loaded", dtos.Count);
        }

        public void Execute(Output output, params string[] tokens)
        {
            if (tokens[0] == "daemon")
            {
                var named = new NamedOutput("MANAGER", output);

                if (tokens.Length == 2 && tokens[1] == "status")
                {
                    controller.Execute(output, tokens);
                }
                if (tokens.Length == 2 && tokens[1] == "list")
                {
                    Execute(named, () => ExecuteList(output, named, tokens));
                }
                if (tokens.Length == 3 && tokens[1] == "install")
                {
                    Execute(named, () => ExecuteInstall3(output, named, tokens));
                }
                if ((tokens.Length == 4 || tokens.Length == 5) && tokens[1] == "install")
                {
                    Execute(named, () => ExecuteInstall45(output, named, tokens));
                }
                if (tokens.Length == 3 && tokens[1] == "uninstall")
                {
                    Execute(named, () => ExecuteUninstall(output, named, tokens));
                }
            }
        }

        private void ExecuteList(Output output, NamedOutput named, params string[] tokens)
        {
            named.Output("Id|Path|Args");
            var dtos = Database.List(dbpath);
            foreach (var dto in dtos)
            {
                named.Output("{0}|{1}|{2}", dto.Id, dto.Path, dto.Args);
            }
            named.Output("{0} daemon(s)", dtos.Count);
        }

        private void ExecuteUninstall(Output output, NamedOutput named, params string[] tokens)
        {
            var id = tokens[2];
            named.Output("Daemon {0} uninstalling...", id);
            Database.Remove(dbpath, id);
            controller.Execute(output, tokens);
        }

        private void ExecuteInstall45(Output output, NamedOutput named, params string[] tokens)
        {
            var dto = new DaemonDto
            {
                Id = tokens[2],
                Path = tokens[3],
                Args = tokens.Length > 4 ? tokens[4] : string.Empty,
            };
            named.Output("Daemon {0} installing {1}|{2}...", dto.Id, dto.Path, dto.Args);
            Database.Save(dbpath, dto);
            controller.Execute(output, tokens);
        }

        private void ExecuteInstall3(Output output, NamedOutput named, params string[] tokens)
        {
            if (Uri.TryCreate(tokens[2], UriKind.Absolute, out var uri))
            {
                var zipfile = Path.GetFileName(uri.LocalPath);
                var zipfilepath = Path.Combine(downloads, zipfile);
                var zipdir = zipfile.Replace(".", "_");
                var zipdirpath = Path.Combine(downloads, zipdir);
                if (Path.GetExtension(zipfile) == ".zip")
                {
                    named.Output("Downloading {0}...", uri);
                    using (var client = new WebClient()) client.DownloadFile(uri, zipfilepath);
                    Directory.CreateDirectory(zipdirpath);
                    ZipFile.ExtractToDirectory(zipfilepath, zipdirpath);
                    var exeargs = File.ReadAllText(Path.Combine(zipdirpath, "Arguments.txt")).Trim();
                    var exefile = File.ReadAllText(Path.Combine(zipdirpath, "Main.txt")).Trim();
                    var exepath = Path.Combine(zipdir, exefile); //relative
                    Execute(output, "daemon", "install", zipfile, exepath, exeargs);
                }
            }
        }

        private void Execute(NamedOutput named, Action action) => runner.Run(action, named.OnException);
    }
}