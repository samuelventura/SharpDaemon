using System;
using System.Text;
using System.Runtime.InteropServices;

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
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        }

        public static string Executable(string format, params object[] args)
        {
            var name = TextTools.Format(format, args);
            return string.Format("{0}{1}", name, IsWindows() ? ".exe" : "");
        }
    }
}
