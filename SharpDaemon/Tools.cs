using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;

namespace SharpDaemon
{
    public static class Stdio
    {
        private static readonly object locker = new object();

        public static void WriteLine(string format, params object[] args)
        {
            lock (locker)
            {
                Console.WriteLine(format, args);
                Console.Out.Flush();
            }
        }

        public static string ReadLine() => Console.ReadLine();
    }

    public class ProcessException : Exception
    {
        private readonly string trace;

        public ProcessException(string message, string trace) : base(message)
        {
            this.trace = trace;
        }

        public string Trace { get { return trace; } }
    }

    public static class Tools
    {
        public static void ExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            ExceptionHandler(args.ExceptionObject as Exception);
        }

        public static void ExceptionHandler(Exception ex)
        {
            Tools.Try(() => Tools.Dump(ex));
            Tools.Try(() => Stdio.WriteLine("!{0}", ex.ToString()));
            Environment.Exit(1);
        }

        public static void Try(Action action, Action<Exception> handler = null)
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                if (handler != null)
                {
                    Try(() => { handler(ex); });
                }
            }
        }

        public static Exception Make(string format, params object[] args)
        {
            var line = format;
            if (args.Length > 0) line = string.Format(format, args);
            return new Exception(line);
        }

        public static string Relative(string child)
        {
            var exe = Assembly.GetExecutingAssembly().Location;
            var root = Path.GetDirectoryName(exe);
            return Path.Combine(root, child);
        }

        public static void Dump(Exception ex)
        {
            var folder = Relative("Exceptions");
            Directory.CreateDirectory(folder);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            var proc = Process.GetCurrentProcess();
            var file = string.Format("Exception-{0}-{1}-{2:000000}.txt", timestamp, proc.ProcessName, proc.Id);
            var path = Path.Combine(folder, file);
            File.WriteAllText(path, ex.ToString());
        }

        public static string Readable(string text)
        {
            var sb = new StringBuilder();
            foreach (var c in text)
            {
                if (Char.IsControl(c)) sb.Append(((int)c).ToString("X2"));
                else if (Char.IsWhiteSpace(c)) sb.Append(((int)c).ToString("X2"));
                else sb.Append(c);
            }
            return sb.ToString();
        }

        public static void Assert(bool condition, string format, params object[] args)
        {
            if (!condition) throw Make(format, args);
        }
    }

    public class Disposer : IDisposable
    {
        private Stack<Action> actions;
        private Action<Exception> handler;

        public Disposer(Action<Exception> handler = null)
        {
            this.handler = handler;
            this.actions = new Stack<Action>();
        }

        public void Push(IDisposable disposable)
        {
            actions.Push(disposable.Dispose);
        }

        public void Push(Action action)
        {
            actions.Push(action);
        }

        public void Dispose()
        {
            while (actions.Count > 0) Tools.Try(actions.Pop(), handler);
        }

        public void Clear()
        {
            actions.Clear();
        }
    }
}
