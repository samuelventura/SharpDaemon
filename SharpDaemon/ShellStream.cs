using System;
using System.Threading;

namespace SharpDaemon
{
    public class ShellStream : IStream, IDisposable
    {
        private readonly LockedQueue<string> queue = new LockedQueue<string>();
        private readonly IWriteLine writer;
        private readonly IReadLine reader;
        private readonly Runner runner;
        private volatile bool closed;

        public ShellStream(IWriteLine writer, IReadLine reader)
        {
            var thread = Thread.CurrentThread.Name;
            this.writer = writer;
            this.reader = reader;
            runner = new Runner(new Runner.Args() { ThreadName = string.Format("{0}_SHELL_R", thread) });
            runner.Run(ReadLoop);
            runner.Run(AfterLoop);
        }

        //reader externally disposed before this
        public void Dispose()
        {
            runner.Dispose();
        }

        //must keep returning null after reader closed
        //to ensure next readline returns null after nested reads
        public string ReadLine()
        {
            if (closed) return null;
            var line = queue.Pop(1, null);
            while (line == null) line = queue.Pop(1, null);
            if (line == Environ.NewLines) closed = true;
            if (line == Environ.NewLines) return null;
            return line;
        }

        //must keep returning null after reader closed
        //to ensure next readline returns null after nested reads
        public string TryReadLine(out bool eof)
        {
            eof = true;
            if (closed) return null;
            eof = false;
            var line = queue.Pop(1, null);
            if (line == Environ.NewLines) closed = true;
            if (line != Environ.NewLines) return line;
            eof = true;
            return null;
        }

        public void HandleException(Exception ex)
        {
            Logger.Trace("{0}", ex);
            writer.WriteLine("{0}", ex);
        }

        public void WriteLine(string format, params object[] args)
        {
            Logger.Trace(format, args);
            writer.WriteLine(format, args);
        }

        private void ReadLoop()
        {
            var line = reader.ReadLine();
            while (line != null)
            {
                Logger.Trace("< {0}", line);
                queue.Push(line);
                line = reader.ReadLine();
            }
        }

        private void AfterLoop()
        {
            queue.Push(Environ.NewLines);
        }
    }
}