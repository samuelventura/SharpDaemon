using System;
using System.Text;
using System.Threading;

namespace SharpDaemon
{
    public static class Stdio
    {
        private static readonly object read = new object();

        public static string ReadLine()
        {
            lock (read) return Console.ReadLine();
        }

        private static readonly object write = new object();

        public static void WriteLine(string format, params object[] args)
        {
            var text = TextTools.Format(format, args);

            lock (write)
            {
                Console.Write(text);
                Console.Write(Environ.NewLine);
                Console.Out.Flush();
            }
        }
    }

    public class ConsoleReadLine : IReadLine
    {
        public string ReadLine()
        {
            return Stdio.ReadLine();
        }
    }

    public class ConsoleWriteLine : IWriteLine
    {
        public void WriteLine(string format, params object[] args)
        {
            Stdio.WriteLine(format, args);
        }
    }

    //Console.ReadLine blocks Console.WriteLine
    //Console.In cannot be closed
    //A read loop in a runner cannot be stopped
    public class RawConsoleReadLine : IReadLine, IDisposable
    {
        public volatile bool disposed;

        public void Dispose() => disposed = true;

        public string ReadLine()
        {
            var sb = new StringBuilder();

            while (true)
            {
                var c = Read();
                if (c == Environ.EndOfFile) return null;
                if (c == Environ.NewLine) return sb.ToString();
                sb.Append((char)c);
            }
        }

        private int Read()
        {
            while (true)
            {
                if (disposed) return Environ.EndOfFile;
                if (Console.KeyAvailable) ReadKey();
                Thread.Sleep(1);
            }
        }

        private int ReadKey()
        {
            return (int)Console.ReadKey().KeyChar;
        }
    }
}
