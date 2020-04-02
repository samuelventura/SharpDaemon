using System;
using System.IO;
using System.Net;
using System.Net.Mime;
using System.IO.Compression;

namespace SharpDaemon.Server
{
    public class DownloadScriptable : IScriptable
    {
        private readonly string downloads;

        public DownloadScriptable(string downloads)
        {
            this.downloads = Path.GetFullPath(downloads); //canonic
        }

        public void Execute(Output output, params string[] tokens)
        {
            if (tokens[0] == "download")
            {
                if (tokens.Length == 2 && tokens[1] == "list")
                {
                    ExecuteList(output, tokens);
                }
                if (tokens.Length == 3 && tokens[1] == "list")
                {
                    ExecuteList(output, tokens);
                }
                if (tokens.Length == 3 && tokens[1] == "delete")
                {
                    ExecuteDelete(output, tokens);
                }
                if (tokens.Length == 4 && tokens[1] == "rename")
                {
                    ExecuteRename(output, tokens);
                }
                if (tokens.Length == 3 && tokens[1] == "zip")
                {
                    ExecuteZip(output, tokens);
                }
            }
            if (tokens[0] == "help")
            {
                output.WriteLine("download zip <uri>");
                output.WriteLine("download list");
                output.WriteLine("download list <path>");
                output.WriteLine("download delete <path>");
                output.WriteLine("download rename <path> <newname>");
            }
        }

        private void ExecuteList(Output output, params string[] tokens)
        {
            var path = downloads;
            if (tokens.Length == 3)
            {
                var dir = tokens[2];
                path = Tools.Combine(downloads, dir);
                Tools.Assert(path.Contains(downloads), "Back navigation not allowed");
                Tools.Assert(Directory.Exists(path), "Directoy doesn't exist {0}", dir);
            }

            var total = 0;
            foreach (var file in Directory.GetDirectories(path))
            {
                total++; //remove final \ as well
                output.WriteLine("{0}", file.Substring(path.Length + 1));
            }
            foreach (var file in Directory.GetFiles(path))
            {
                total++; //remove final \ as well
                var furi = new Uri(file);
                output.WriteLine("{0}", file.Substring(path.Length + 1));
            }
            output.WriteLine("{0} total files", total);
        }

        private void ExecuteDelete(Output output, params string[] tokens)
        {
            var dir = tokens[2];
            var path = Tools.Combine(downloads, dir);
            Tools.Assert(Tools.IsChild(downloads, dir), "Back navigation not allowed");
            Tools.Assert(Directory.Exists(path), "Directoy doesn't exist {0}", dir);
            Directory.Delete(path, true);
        }

        private void ExecuteRename(Output output, params string[] tokens)
        {
            var dir = tokens[2];
            var path = Tools.Combine(downloads, dir);
            Tools.Assert(Tools.IsChild(downloads, dir), "Back navigation not allowed");
            Tools.Assert(Directory.Exists(path), "Directoy doesn't exist {0}", dir);

            var name = tokens[3];
            var parent = Path.GetDirectoryName(path);
            var npath = Path.Combine(parent, name);
            var ndir = npath.Substring(downloads.Length + 1);
            Tools.Assert(!Directory.Exists(npath), "Directoy alreay exist {0}", ndir);
            Directory.Move(path, npath);
        }

        private void ExecuteZip(Output output, params string[] tokens)
        {
            var uri = new Uri(tokens[2], UriKind.Absolute);
            var request = (HttpWebRequest)WebRequest.Create(uri);
            using (var response = (HttpWebResponse)request.GetResponse())
            {
                //https://www.nuget.org/api/v2/package/SharpSerial/1.0.1
                //redirects to https://globalcdn.nuget.org/packages/sharpserial.1.0.1.nupkg
                output.WriteLine("Response URI : {0}...", response.ResponseUri);
                var zipfile = Path.GetFileName(response.ResponseUri.LocalPath);

                //Content-Disposition: attachment; filename="fname.ext"
                //nugets get application/octet-stream and empty disposition
                //to test Content-Disposition header use google drive link
                //SharpDaemon-sample.zip uploaded to drive with public link
                //https://docs.google.com/uc?export=download&id=1YgDnibq0waSbCYsobl3SIP_dSgD5DYbl
                var disposition = response.Headers["Content-Disposition"];
                var mime = response.Headers["Content-Type"];
                output.WriteLine("Content-Disposition : {0}...", disposition);
                output.WriteLine("Content-Type : {0}...", mime);
                if (!string.IsNullOrEmpty(disposition))
                {
                    var header = new ContentDisposition(disposition);
                    if (!string.IsNullOrEmpty(header.FileName)) zipfile = header.FileName;
                }
                var zipdir = zipfile.Replace(".", "_");
                output.WriteLine("Extracting to {0}...", zipdir);
                var zipdirpath = Path.Combine(downloads, zipdir);
                Tools.Assert(!Directory.Exists(zipdirpath), "Download directory already exists {0}", zipdir);
                using (var zip = new ZipArchive(response.GetResponseStream()))
                {
                    Directory.CreateDirectory(zipdirpath);
                    zip.ExtractToDirectory(zipdirpath);
                    output.WriteLine("Downloading finished");
                }
            }
        }
    }
}