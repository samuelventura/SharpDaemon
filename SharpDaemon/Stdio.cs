using System;

namespace SharpDaemon
{
    public static class Stdio
    {
        private static readonly object locker = new object();

        public static void WriteLine(string format, params object[] args)
        {
            var text = Tools.Format(format, args).Replace('\n', '|');

            lock (locker)
            {
                Console.WriteLine(text);
                Console.Out.Flush();
            }
        }

        public static string ReadLine() => Console.ReadLine();
    }
}
