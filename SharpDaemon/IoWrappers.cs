using System;
using System.IO;
using System.Collections.Generic;

namespace SharpDaemon
{
    public class WriteLineCollection : IWriteLine
    {
        private readonly List<IWriteLine> writers = new List<IWriteLine>();

        public void Add(IWriteLine writer) => writers.Add(writer);

        public void WriteLine(string format, params object[] args)
        {
            var text = TextTools.Format(format, args);

            foreach (var writer in writers)
            {
                try { writer.WriteLine(text); }
                catch (Exception) { /* IGNORE */ }
            }
        }
    }

    public class TextWriterWriteLine : IWriteLine
    {
        private readonly object locker = new object();
        private readonly TextWriter writer;

        public TextWriterWriteLine(TextWriter writer) => this.writer = writer;

        public void WriteLine(string format, params object[] args)
        {
            var text = TextTools.Format(format, args);

            lock (locker)
            {
                writer.Write(text);
                writer.Write(Environ.NewLine);
                writer.Flush();
            }
        }
    }

    public class TextReaderReadLine : IReadLine
    {
        private readonly object locker = new object();
        private readonly TextReader reader;

        public TextReaderReadLine(TextReader reader) => this.reader = reader;

        public string ReadLine()
        {
            lock (locker) return reader.ReadLine();
        }
    }

    public class TimedWriter : IWriteLine
    {
        private readonly IWriteLine writer;
        private readonly string dtf;

        public TimedWriter(IWriteLine writer, string dtf = "yyyy-MM-dd HH:mm:ss.fff")
        {
            this.writer = writer;
            this.dtf = dtf;
        }

        public void WriteLine(string format, params object[] args)
        {
            var dtt = DateTime.Now.ToString(dtf);
            var text = TextTools.Format(format, args);
            writer.WriteLine("{0} {1}", dtt, text);
        }
    }
}
