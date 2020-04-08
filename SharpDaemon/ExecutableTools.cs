using System;
using System.IO;
using System.Reflection;

namespace SharpDaemon
{
    public static class ExecutableTools
    {
        public static string Relative(string format, params object[] args)
        {
            var child = TextTools.Format(format, args);
            return Path.Combine(Directory(), child);
        }

        public static string Directory()
        {
            var exe = Assembly.GetExecutingAssembly().Location;
            return Path.GetDirectoryName(exe);
        }
    }
}
