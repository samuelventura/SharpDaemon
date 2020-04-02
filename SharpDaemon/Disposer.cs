using System;
using System.Threading;
using System.Collections.Generic;

namespace SharpDaemon
{
    public abstract class Disposable : IDisposable
    {
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
            public override string ToString() => $"${Id}:${Name}";
        }

        private readonly ThreadInfo thread = new ThreadInfo();
        private readonly object locker = new object();
        private volatile bool disposed;

        public bool Disposed { get { return disposed; } }

        public Disposable() => Counter.Plus(this);

        public void Dispose()
        {
            lock (locker)
            {
                var t = new ThreadInfo();
                Tools.Assert(t.Id == thread.Id, "Thread mismatch d:{0} != c:{1}", t, thread);
                if (!disposed)
                {
                    Tools.Try(() => Dispose(disposed));
                    Counter.Minus(this);
                    disposed = true;
                }
            }
        }

        protected abstract void Dispose(bool disposed);
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
