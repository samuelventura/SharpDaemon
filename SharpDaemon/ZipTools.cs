using System;
using System.IO;
using System.IO.Compression;

namespace SharpDaemon
{
    public static class ZipTools
    {
        public static void EntryFromString(this ZipArchive zip, string name, string text)
        {
            var entry = zip.CreateEntry(name);
            using (var writer = new StreamWriter(entry.Open())) writer.Write(text);
        }

        public static void ZipFromFiles(string zippath, string root, params string[] files)
        {
            using (var zip = ZipFile.Open(zippath, ZipArchiveMode.Create))
            {
                foreach (var file in files)
                {
                    var path = Path.Combine(root, file);
                    //Output.Trace("Adding to ZIP {0}", path);
                    zip.CreateEntryFromFile(path, file);
                }
            }
        }
    }
}
