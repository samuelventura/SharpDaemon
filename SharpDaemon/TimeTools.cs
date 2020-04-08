using System;
using System.Text;

namespace SharpDaemon
{
    public static class TimeTools
    {
        public static string Compact(DateTime dt) => dt.ToString("yyyyMMdd_HHmmss_fff");
        public static string Format(DateTime dt) => dt.ToString("yyyy-MM-dd HH:mm:ss.fff");

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
    }
}
