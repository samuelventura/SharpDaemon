using System;

namespace SharpDaemon
{
    public static class Logger
    {
        public static void Error(string format, params object[] args) => Log("E", format, args);
        public static void Success(string format, params object[] args) => Log("S", format, args);
        public static void Warn(string format, params object[] args) => Log("W", format, args);
        public static void Info(string format, params object[] args) => Log("I", format, args);
        public static void Debug(string format, params object[] args) => Log("D", format, args);
        public static void Trace(string format, params object[] args) => Log("T", format, args);

        public static void Log(string level, string format, params object[] args)
        {
            var text = format;
            var ts = DateTime.Now.Ticks;
            if (args.Length > 0) text = string.Format(format, args);
            foreach (var line in text.Split('\n')) Stdio.WriteLine("#{0} {1} {2}", level, ts, text);
        }
    }

    public class Log
    {
        public string Level;
        public DateTime Timestamp;
        public string Message;

        public static void Parse(Log log, string line)
        {
            var parts = line.Split(new char[] { ' ' }, 3);
            log.Level = ParseLevel(parts[0]);
            log.Timestamp = new DateTime(long.Parse(parts[1]));
            log.Message = parts[2];
        }

        public static string ParseLevel(string prefix)
        {
            switch (prefix)
            {
                case "#E": return "Error";
                case "#S": return "Success";
                case "#W": return "Warn";
                case "#I": return "Info";
                case "#D": return "Debug";
                case "#T": return "Trace";
            }
            throw Tools.Make("Unknown prefix {0}", Tools.Readable(prefix));
        }
    }
}
