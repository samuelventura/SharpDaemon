using System;
using System.IO;
using System.Collections.Generic;

namespace SharpDaemon
{
    public interface Output
    {
        void WriteLine(string format, params object[] args);
    }

    public class Outputs : Output
    {
        private readonly List<Output> outputs = new List<Output>();

        public void Add(Output output)
        {
            outputs.Add(output);
        }

        public void WriteLine(string format, params object[] args)
        {
            var dtt = Tools.Format(DateTime.Now);
            var text = Tools.Format(format, args);

            var remove = new List<Output>();
            foreach (var output in outputs)
            {
                Tools.Try(() => output.WriteLine("{0} {1}", dtt, text), (ex) =>
                {
                    remove.Add(output);
                });
            }
            foreach (var output in remove)
            {
                outputs.Remove(output);
                if (output is IDisposable)
                {
                    var disposable = output as IDisposable;
                    Tools.Try(disposable.Dispose);
                }
            }
        }
    }

    public class NamedOutput : Output
    {
        private readonly Output output;
        private readonly string name;

        public Output Output { get { return output; } }

        public NamedOutput(string name, Output output)
        {
            this.name = name;
            this.output = output;
        }

        public void WriteLine(string format, params object[] args)
        {
            var text = Tools.Format(format, args);
            output.WriteLine("{0} {1}", name, text);
        }

        public void OnException(Exception ex)
        {
            output.WriteLine("{0} {1}", name, ex.ToString());
        }
    }

    public class StdOutput : Output
    {
        private readonly object locker = new object();

        public void WriteLine(string format, params object[] args)
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

        public void WriteLine(string format, params object[] args)
        {
            lock (locker)
            {
                writer.WriteLine(format, args);
                writer.Flush();
            }
        }
    }
}
