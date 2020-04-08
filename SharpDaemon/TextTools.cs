using System;
using System.Text;

namespace SharpDaemon
{
    public static class TextTools
    {
        public static string Format(string format, params object[] args)
        {
            if (args.Length > 0) return string.Format(format, args);
            return format;
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
    }
}
