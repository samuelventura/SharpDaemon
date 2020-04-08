using System;

namespace SharpDaemon
{
    public class ShellStream : IStream, IDisposable
    {
        private readonly LockedQueue<string> queue = new LockedQueue<string>();
        private readonly string newline = $"{Environ.NewLine}";
        private readonly IOutput output;
        private readonly IReadLine reader;
        private readonly Runner runner;
        private volatile bool closed;

        public ShellStream(IOutput output, IReadLine reader)
        {
            this.output = output;
            this.reader = reader;
            runner = new Runner();
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
            if (line == newline) closed = true;
            if (line == newline) return null;
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
            if (line == newline) closed = true;
            if (line != newline) return line;
            eof = true;
            return null;
        }

        public void HandleException(Exception ex) => output.HandleException(ex);

        public void WriteLine(string format, params object[] args) => output.WriteLine(format, args);

        private void ReadLoop()
        {
            var line = reader.ReadLine();
            while (line != null)
            {
                queue.Push(line);
                line = reader.ReadLine();
            }
        }

        private void AfterLoop()
        {
            queue.Push(newline);
        }
    }
}