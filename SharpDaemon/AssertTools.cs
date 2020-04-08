using System;
using System.Net;

namespace SharpDaemon
{
    public static class AssertTools
    {
        public static void True(bool condition, string format, params object[] args)
        {
            if (!condition) throw ExceptionTools.Make(format, args);
        }

        public static void NotEmpty(string text, string format, params object[] args)
        {
            if (string.IsNullOrWhiteSpace(text)) throw ExceptionTools.Make(format, args);
        }

        public static void Endpoint(string text, string format, params object[] args)
        {
            var parts = text.Split(':');
            if (parts.Length != 2) throw ExceptionTools.Make(format, args);
            if (!IPAddress.TryParse(parts[0], out var ip)) throw ExceptionTools.Make(format, args);
            if (!int.TryParse(parts[1], out var p)) throw ExceptionTools.Make(format, args);
        }

        public static void Ip(string text, string format, params object[] args)
        {
            if (!IPAddress.TryParse(text, out var ip)) throw ExceptionTools.Make(format, args);
        }
    }
}
