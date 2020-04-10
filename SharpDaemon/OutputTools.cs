using System;
using System.Threading;

namespace SharpDaemon
{
    public interface IOutput : IWriteLine, IHandleException { }

    public class Output : IOutput
    {
        public static IWriteLine TRACE;

        public static void Trace(string format, params object[] args)
        {
            if (TRACE != null)
            {
                var text = TextTools.Format(format, args);
                var thread = Thread.CurrentThread;
                TRACE.WriteLine("Thread:{0}:{1} {2}"
                    , thread.ManagedThreadId
                    , thread.Name
                    , text
                );
            }
        }

        public static void Trace(Exception ex) => Trace("{0}", ex);

        private readonly IWriteLine writer;

        public Output(IWriteLine writer)
        {
            this.writer = writer;
        }

        public void HandleException(Exception ex) => WriteLine("{0}", ex);

        public void WriteLine(string format, params object[] args)
        {
            writer.WriteLine(format, args);
        }
    }

    public class NamedOutput : IOutput
    {
        private readonly IWriteLine writer;
        private readonly string name;

        public NamedOutput(IWriteLine writer, string name)
        {
            this.writer = writer;
            this.name = name;
        }

        public void HandleException(Exception ex) => WriteLine("{0}", ex);

        public void WriteLine(string format, params object[] args)
        {
            var text = TextTools.Format(format, args);
            writer.WriteLine("{0} {1}", name, text);
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
