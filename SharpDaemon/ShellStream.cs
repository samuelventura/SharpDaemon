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

        public ShellStream(IOutput output, IReadLine reader)
        {
            this.output = output;
            this.reader = reader;
            runner = new Runner();
            runner.Run(ReadLoop);
            runner.Run(() => queue.Push(newline));
        }

        //reader externally disposed before this
        public void Dispose()
        {
            runner.Dispose();
        }

        public string ReadLine()
        {
            var line = queue.Pop(1, null);
            while (line == null) line = queue.Pop(1, null);
            if (line == newline) return null;
            return line;
        }

        public string TryReadLine(out bool eof)
        {
            eof = false;
            var line = queue.Pop(1, null);
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
    }
}