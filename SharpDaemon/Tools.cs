using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Net.Sockets;
using System.IO.Compression;
using System.Security.Principal;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SharpDaemon
{
    public static class Tools
    {
        //https://stackoverflow.com/questions/35280597/net-tcplistener-stop-method-does-not-stop-the-listener-when-there-is-a-child-pr
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetHandleInformation(IntPtr hObject, uint dwMask, uint dwFlags);
        private const uint HANDLE_FLAG_INHERIT = 1;

        public static void MakeNotInheritable(this TcpListener tcpListener)
        {
            var handle = tcpListener.Server.Handle;
            SetHandleInformation(handle, HANDLE_FLAG_INHERIT, 0);
        }

        public static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static string Compact(DateTime dt) => dt.ToString("yyyyMMdd_HHmmss_fff");
        public static string Format(DateTime dt) => dt.ToString("yyyy-MM-dd HH:mm:ss.fff");
        public static string Format(string format, params object[] args)
        {
            if (args.Length > 0) return string.Format(format, args);
            return format;
        }

        public static string Format(double totalSeconds)
        {
            var wholeSeconds = (long)Math.Floor(totalSeconds);
            var partialSeconds = totalSeconds - wholeSeconds;
            var hours = wholeSeconds / 3600;
            var minutes = (wholeSeconds % 3600) / 60;
            var seconds = wholeSeconds % 60 + partialSeconds;
            var sb = new StringBuilder();
            if (hours > 0) sb.AppendFormat("{0}h", hours);
            if (minutes > 0) sb.AppendFormat("{0}m", minutes);
            sb.AppendFormat("{0:0.0}s", seconds);
            return sb.ToString();
        }

        //checks against existing subfolders
        public static bool HasDirectChild(string folder, string child)
        {
            var root = Path.GetFullPath(folder);
            var children = new List<string>(Directory.GetDirectories(root));
            return children.Contains(Combine(folder, child));
        }

        //path level check
        public static bool IsChildPath(string folder, string child)
        {
            var root = Path.GetFullPath(folder);
            var path = Combine(folder, child);
            //check there is something after root
            if (path.Length <= root.Length) return false;
            //check root is parent
            if (!path.Contains(root)) return false;
            //check child name contains no navigation leading somewhere else
            //length + 1 to remove path separator as well
            if (child != path.Substring(root.Length + 1)) return false;
            return true;
        }

        public static string Combine(string folder, string relative)
        {
            var root = Path.GetFullPath(folder);
            return Path.GetFullPath(Path.Combine(root, relative));
        }

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
                    var path = Combine(root, file);
                    zip.CreateEntryFromFile(path, file);
                }
            }
        }

        public static void ExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            ExceptionHandler(args.ExceptionObject as Exception);
        }

        public static void ExceptionHandler(Exception ex)
        {
            Tools.Try(() => Tools.Dump(ex));
            Tools.Try(() => Stdio.WriteLine("{0} {1}", ex.GetType(), ex.Message));
            Environment.Exit(1);
        }

        public static void Try(Action action, Action<Exception> handler = null)
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                if (handler != null)
                {
                    Try(() => { handler(ex); });
                }
            }
        }

        public static Exception Make(string format, params object[] args)
        {
            return new Exception(Format(format, args));
        }

        public static string Relative(string format, params object[] args)
        {
            var child = Format(format, args);
            var exe = Assembly.GetExecutingAssembly().Location;
            var root = Path.GetDirectoryName(exe);
            return Path.Combine(root, child);
        }

        public static string ExecutableDirectory()
        {
            var exe = Assembly.GetExecutingAssembly().Location;
            return Path.GetDirectoryName(exe);
        }

        public static void Dump(Exception ex)
        {
            var folder = Relative("Exceptions");
            Directory.CreateDirectory(folder);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            var proc = Process.GetCurrentProcess();
            var file = string.Format("Exception-{0}-{1}-{2:000000}.txt", timestamp, proc.ProcessName, proc.Id);
            var path = Path.Combine(folder, file);
            File.WriteAllText(path, ex.ToString());
        }

        public static string Readable(string text)
        {
            var sb = new StringBuilder();
            foreach (var c in text)
            {
                if (Char.IsControl(c)) sb.Append(((int)c).ToString("X2"));
                else if (Char.IsWhiteSpace(c)) sb.Append(((int)c).ToString("X2"));
                else sb.Append(c);
            }
            return sb.ToString();
        }

        public static void Assert(bool condition, string format, params object[] args)
        {
            if (!condition) throw Make(format, args);
        }

        public static void SetProperty(object target, string line)
        {
            var parts = line.Split(new char[] { '=' });
            if (parts.Length != 2) throw Make("Expected 2 parts in {0}", Readable(line));
            var propertyName = parts[0];
            var propertyValue = parts[1];
            var property = target.GetType().GetProperty(propertyName);
            if (property == null) throw Make("Property not found {0}", Readable(propertyName));
            var value = Convert.ChangeType(propertyValue, property.PropertyType);
            property.SetValue(target, value, null);
        }
    }
}
