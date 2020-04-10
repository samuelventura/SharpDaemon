using System;
using System.IO;
using System.Net;
using System.Net.Mime;
using System.IO.Compression;

namespace SharpDaemon.Server
{
    public class DownloadScriptable : IShell
    {
        private readonly string downloads;

        public DownloadScriptable(string downloads)
        {
            this.downloads = Path.GetFullPath(downloads); //canonic
        }

        //should only support zip download and folder level commands
        //should prevent any parent folder access
        public void Execute(IStream io, params string[] tokens)
        {
            if (tokens[0] == "download")
            {
                if (tokens.Length == 2 && tokens[1] == "list")
                {
                    ExecuteList(io, tokens);
                }
                if (tokens.Length == 3 && tokens[1] == "list")
                {
                    ExecuteList(io, tokens);
                }
                if (tokens.Length == 3 && tokens[1] == "delete")
                {
                    ExecuteDelete(io, tokens);
                }
                if (tokens.Length == 4 && tokens[1] == "rename")
                {
                    ExecuteRename(io, tokens);
                }
                if (tokens.Length == 3 && tokens[1] == "zip")
                {
                    ExecuteZip(io, tokens);
                }
            }
            if (tokens[0] == "help")
            {
                io.WriteLine("download zip <uri>");
                io.WriteLine("download list");
                io.WriteLine("download list <folder-name>");
                io.WriteLine("download delete <folder-name>");
                io.WriteLine("download rename <folder-name> <new-name>");
            }
        }

        private void ExecuteList(IOutput io, params string[] tokens)
        {
            var path = downloads;
            if (tokens.Length == 3)
            {
                var dir = tokens[2];
                AssertTools.True(PathTools.HasDirectChild(downloads, dir), "Directory not found {0}", dir);
                path = PathTools.Combine(downloads, dir);
            }

            var total = 0;
            foreach (var file in Directory.GetDirectories(path))
            {
                total++; //remove final \ as well
                io.WriteLine("{0}", file.Substring(path.Length + 1));
            }
            io.WriteLine("{0} total directories", total);
            total = 0;
            foreach (var file in Directory.GetFiles(path))
            {
                total++; //remove final \ as well
                var furi = new Uri(file);
                io.WriteLine("{0}", file.Substring(path.Length + 1));
            }
            io.WriteLine("{0} total files", total);
        }

        private void ExecuteDelete(IOutput io, params string[] tokens)
        {
            var dir = tokens[2];
            AssertTools.True(PathTools.HasDirectChild(downloads, dir), "Directory {0} not found", dir);
            Directory.Delete(PathTools.Combine(downloads, dir), true);
            io.WriteLine("Directory {0} deleted", dir);
        }

        //https://stackoverflow.com/questions/18924789/directory-move-access-to-path-is-denied
        //netcore linux WSL System.IO.IOException: Access to the path
        //download zip and delete work ok at the same time
        private void ExecuteRename(IOutput io, params string[] tokens)
        {
            var dir = tokens[2];
            AssertTools.True(PathTools.HasDirectChild(downloads, dir), "Directory {0} not found", dir);
            var path = PathTools.Combine(downloads, dir);

            var name = tokens[3];
            AssertTools.True(PathTools.IsChildPath(downloads, name), "Invalid new name {0}", name);
            var npath = PathTools.Combine(downloads, name);
            AssertTools.True(!Directory.Exists(npath), "Directory {0} already exist", name);

            Directory.Move(path, npath);
            io.WriteLine("Directory {0} renamed to {1}", dir, name);
        }

        private void ExecuteZip(IOutput io, params string[] tokens)
        {
            var uri = new Uri(tokens[2], UriKind.Absolute);
            var request = (HttpWebRequest)WebRequest.Create(uri);
            request.Timeout = 5000;
            using (var response = (HttpWebResponse)request.GetResponse())
            {
                //https://www.nuget.org/api/v2/package/SharpSerial/1.0.1
                //redirects to https://globalcdn.nuget.org/packages/sharpserial.1.0.1.nupkg
                //io.WriteLine("Response URI : {0}...", response.ResponseUri);
                var zipfile = Path.GetFileName(response.ResponseUri.LocalPath);

                //Content-Disposition: attachment; filename="fname.ext"
                //nugets get application/octet-stream and empty disposition
                //to test Content-Disposition header use google drive link
                //SharpDaemon-sample.zip uploaded to drive with public link
                //https://docs.google.com/uc?export=download&id=1YgDnibq0waSbCYsobl3SIP_dSgD5DYbl
                var disposition = response.Headers["Content-Disposition"];
                //var mime = response.Headers["Content-Type"];
                //io.WriteLine("Content-Disposition : {0}...", disposition);
                //io.WriteLine("Content-Type : {0}...", mime);
                if (!string.IsNullOrEmpty(disposition))
                {
                    var header = new ContentDisposition(disposition);
                    if (!string.IsNullOrEmpty(header.FileName)) zipfile = header.FileName;
                }
                var zipdir = zipfile; //do not remove dots
                var zipdirpath = Path.Combine(downloads, zipdir);
                AssertTools.True(PathTools.IsChildPath(downloads, zipdir), "Invalid directory name {0}", zipdir);
                AssertTools.True(!Directory.Exists(zipdirpath), "Download directory already exists {0}", zipdir);
                using (var zip = new ZipArchive(response.GetResponseStream()))
                {
                    Directory.CreateDirectory(zipdirpath);
                    zip.ExtractToDirectory(zipdirpath);
                    io.WriteLine("Downloaded to {0}", zipdir);
                }
            }
        }
    }
}