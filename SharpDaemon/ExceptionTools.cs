using System;
using System.IO;
using System.Diagnostics;

namespace SharpDaemon
{
    public interface IHandleException
    {
        void HandleException(Exception ex);
    }

    public static class ExceptionTools
    {
        public static void DumpAndExit(object sender, UnhandledExceptionEventArgs args)
        {
            DumpAndExit(args.ExceptionObject as Exception);
        }

        public static void DumpAndExit(Exception ex)
        {
            //FIXME this should never happen
            //there is a programming error to be fixed
            Try(() => Dump(ex));
            Try(() => Stdio.SetStatus("{0} {1}", ex.GetType(), ex.Message));
            Try(() => Stdio.LogError(ex));
            Environment.Exit(1);
        }

        public static void Try(Action action, Action<Exception> handler = null)
        {
            try { action.Invoke(); }
            catch (Exception ex) { handler?.Invoke(ex); }
        }

        public static void Dump(Exception ex)
        {
            var folder = ExecutableTools.Relative("Exceptions");
            Directory.CreateDirectory(folder);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            var proc = Process.GetCurrentProcess();
            var file = string.Format("Exception-{0}-{1}-{2:000000}.txt", timestamp, proc.ProcessName, proc.Id);
            var path = Path.Combine(folder, file);
            File.WriteAllText(path, ex.ToString());
        }

        public static Exception Make(string format, params object[] args)
        {
            return new Exception(TextTools.Format(format, args));
        }
    }
}
