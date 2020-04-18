using System;
using System.Threading;
using System.Diagnostics;

namespace SharpDaemon
{
    public class Logger
    {
        public static IWriteLine TRACE;

        public static void Trace(string format, params object[] args)
        {
            if (TRACE != null)
            {
                var pid = Process.GetCurrentProcess().Id;
                var text = TextTools.Format(format, args);
                var thread = Thread.CurrentThread;
                TRACE.WriteLine("TRACE Pid:{0} Thread:{1}:{2} {3}"
                    , pid
                    , thread.ManagedThreadId
                    , thread.Name
                    , text
                );
            }
        }

        public static void Trace(Exception ex) => Trace("{0}", ex);
    }
}
