using System;
using System.IO;
using System.Collections.Generic;

namespace SharpDaemon
{
    public interface Output
    {
        void Output(string format, params object[] args);
    }

    public class Outputs : Output
    {
        private readonly List<Output> outputs = new List<Output>();

        public void Add(Output output)
        {
            outputs.Add(output);
        }

        public void Output(string format, params object[] args)
        {
            var dt = DateTime.Now;
            var text = format;
            if (args.Length > 0) text = string.Format(format, args);
            var dtt = dt.ToString("yyyy-MM-dd HH:mm:ss.fff");

            foreach (var output in outputs)
            {
                Tools.Try(() => output.Output("{0} {1}", dtt, text));
            }
        }

        public void Output(DateTime dt, string format, params object[] args)
        {
            var text = format;
            if (args.Length > 0) text = string.Format(format, args);
            var dtt = dt.ToString("yyyy-MM-dd HH:mm:ss.fff");

            foreach (var output in outputs)
            {
                Tools.Try(() => output.Output("{0} {1}", dtt, text));
            }
        }
    }

    public class StdOutput : Output
    {
        private readonly object locker = new object();

        public void Output(string format, params object[] args)
        {
            lock (locker)
            {
                Console.Out.WriteLine(format, args);
                Console.Out.Flush();
            }
        }
    }

    public class WriterOutput : Output, IDisposable
    {
        private readonly object locker = new object();
        private readonly StreamWriter writer;

        public WriterOutput(StreamWriter writer)
        {
            this.writer = writer;
        }

        public void Dispose()
        {
            Tools.Try(writer.Dispose);
        }

        public void Output(string format, params object[] args)
        {
            lock (locker)
            {
                writer.WriteLine(format, args);
                writer.Flush();
            }
        }
    }
}
