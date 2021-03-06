﻿using System;
using System.IO;
using System.Reflection;

namespace SharpDaemon
{
    public static class ExecutableTools
    {
        public static Assembly GetAssembly()
        {
            //unit entry is null
            return Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        }

        public static string Location()
        {
            return GetAssembly().Location;
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

        public static void LogArgs(IWriteLine writer, string[] args, Action<string> callback = null)
        {
            writer?.WriteLine("Cmd {0}", Location());
            writer?.WriteLine("Args {0} {1}", args.Length, string.Join(" ", args));

            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                writer?.WriteLine("Arg {0} {1}", i, arg);
                callback?.Invoke(arg);
            }
        }
    }
}
