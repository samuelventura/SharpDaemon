using System;

namespace SharpDaemon
{
    public static class Stdio
    {
        private static readonly object locker = new object();

        public static void WriteLine(string format, params object[] args)
        {
            lock (locker)
            {
                Console.WriteLine(format, args);
                Console.Out.Flush();
            }
        }

        public static string ReadLine() => Console.ReadLine();
    }
}
