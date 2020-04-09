using System;
using System.IO;
using System.Reflection;

namespace SharpDaemon
{
    public static class ExecutableTools
    {
        public static Assembly GetAssembly()
        {
            return Assembly.GetExecutingAssembly();
        }

        public static string Relative(string format, params object[] args)
        {
            var child = TextTools.Format(format, args);
            return Path.Combine(Directory(), child);
        }

        public static string Directory()
        {
            var exe = GetAssembly().Location;
            return Path.GetDirectoryName(exe);
        }

        public static DateTime BuildDateTime()
        {
            //http://stackoverflow.com/questions/1600962/displaying-the-build-date
            //does not consider daylight savings time
            var version = GetAssembly().GetName().Version;
            return new DateTime(2000, 1, 1).Add(new TimeSpan(
                TimeSpan.TicksPerDay * version.Build + // days since 1 January 2000
                TimeSpan.TicksPerSecond * 2 * version.Revision)); /* seconds since midnight,
                                                     (multiply by 2 to get original) */
        }

        public static string VersionString()
        {
            return GetAssembly().GetName().Version.ToString();
        }
    }
}
