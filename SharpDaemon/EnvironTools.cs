using System;
using System.IO;
using System.Text;

namespace SharpDaemon
{
    public static class Environ
    {
        public static readonly Encoding Encode = Encoding.UTF8;
        public const char NewLine = '\n';
        public const int EndOfFile = -1;

        public const int InfiniteTo = -1;

        //https://stackoverflow.com/questions/38790802/determine-operating-system-in-net-core
        public static bool IsWindows()
        {
            var windir = Environment.GetEnvironmentVariable("windir");
            if (string.IsNullOrEmpty(windir)) return false;
            if (windir.Contains(@"\")) return false;
            if (Directory.Exists(windir)) return false;
            return true;
        }

        public static string Executable(string format, params object[] args)
        {
            var name = TextTools.Format(format, args);
            return string.Format("{0}{1}", name, IsWindows() ? ".exe" : "");
        }
    }
}
