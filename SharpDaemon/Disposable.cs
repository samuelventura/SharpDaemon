using System;
using System.Threading;

namespace SharpDaemon
{
    public abstract class Disposable : IDisposable
    {

        public class Wrapper : Disposable
        {
            private readonly IDisposable disposable;

            public Wrapper(IDisposable disposable)
            {
                this.disposable = disposable;
            }

            protected override void Dispose(bool disposed)
            {
                disposable.Dispose();
            }
        }
        class ThreadInfo
        {
            public readonly int Id;
            public readonly string Name;
            public ThreadInfo()
            {
                var thread = Thread.CurrentThread;
                Id = thread.ManagedThreadId;
                Name = thread.Name;
            }
            public override string ToString() => $"{Id}:{Name}";
        }

        private static readonly LockedSet<object> undisposed = new LockedSet<object>();
        public static int Undisposed { get { return undisposed.Count; } }
        private readonly ThreadInfo thread = new ThreadInfo();
        private readonly object locker = new object();
        private volatile bool disposed;

        public bool Disposed { get { return disposed; } }

        public Disposable()
        {
            lock (locker)
            {
                Log("Constructor");
                undisposed.Add(this);
                Counter.Plus(this);
            }
        }

        public void Dispose()
        {
            Log("Dispose before lock");
            lock (locker)
            {
                Log("Dispose in lock");
                var t = new ThreadInfo();
                if (t.Id != thread.Id) Log("Thread mismatch d:{0} != c:{1}", t, thread);
                //AssertTools.True(t.Id == thread.Id, "Thread mismatch d:{0} != c:{1}", t, thread);
                if (!disposed)
                {
                    Log("Disposing...");
                    ExceptionTools.Try(() => Dispose(disposed), HandleException);
                    Log("Dispose OK");
                    Counter.Minus(this);
                    disposed = true;
                    var found = undisposed.Remove(this);
                    AssertTools.True(found, "Disposable not found in undisposed");
                    Log("Disposed");
                }
            }
        }

        private void HandleException(Exception ex)
        {
            Log("{0}", ex.ToString());
        }

        private void Log(string format, params object[] args)
        {
            var writer = Logger.TRACE;
            if (writer != null)
            {
                var text = TextTools.Format(format, args);
                var thread = Thread.CurrentThread;
                writer.WriteLine("Thread:{0}:{1} Disposable:{2}:{3} DC:{4} {5}"
                    , thread.ManagedThreadId
                    , thread.Name
                    , GetType()
                    , GetHashCode()
                    , Undisposed
                    , text
                );
            }
        }

        protected abstract void Dispose(bool disposed);
    }
}
