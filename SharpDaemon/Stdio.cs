using System;
using System.IO;
using System.Threading;

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

    //Console.ReadLine block Console.WriteLine
    //Console.In cannot be closed
    //A read loop in a runner cannot be stopped
    public class ConsoleTextReader : TextReader
    {
        public volatile bool disposed;

        public override int Read()
        {
            while (!disposed)
            {
                if (!Console.KeyAvailable)
                {
                    Thread.Sleep(1);
                    continue;
                }
                return (int)Console.ReadKey().KeyChar;
            }
            return -1;
        }

        protected override void Dispose(bool b)
        {
            disposed = true;
        }
    }
}
