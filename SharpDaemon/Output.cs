using System;
using System.IO;
using System.Collections.Generic;

namespace SharpDaemon
{
    public interface IOutput
    {
        void WriteLine(string format, params object[] args);
        void OnException(Exception ex);
    }

    public abstract class Output : IOutput
    {
        public abstract void WriteLine(string format, params object[] args);
        public void OnException(Exception ex) => WriteLine("{0}", ex.ToString());
    }

    public class Outputs : Output
    {
        private readonly List<Output> outputs = new List<Output>();

        public void Add(Output output)
        {
            outputs.Add(output);
        }

        public override void WriteLine(string format, params object[] args)
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

        public override void WriteLine(string format, params object[] args)
        {
            var text = Tools.Format(format, args);
            output.WriteLine("{0} {1}", name, text);
        }
    }

    public class ConsoleOutput : Output
    {
        private readonly object locker = new object();

        public override void WriteLine(string format, params object[] args)
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
        private readonly TextWriter writer;

        public WriterOutput(TextWriter writer)
        {
            this.writer = writer;
        }

        public void Dispose()
        {
            Tools.Try(writer.Dispose);
        }

        public override void WriteLine(string format, params object[] args)
        {
            lock (locker)
            {
                writer.WriteLine(format, args);
                writer.Flush();
            }
        }
    }
}
