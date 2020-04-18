using System;
using System.Threading;

namespace SharpDaemon
{
    public static class ProgramTools
    {
        public static void Setup()
        {
            Thread.CurrentThread.Name = "Main";
            AppDomain.CurrentDomain.UnhandledException += ExceptionTools.DumpAndExit;
        }
    }
}
