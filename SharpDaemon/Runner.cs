using System;
using System.Collections.Generic;
using System.Threading;

namespace SharpDaemon
{
    public class Callback
    {
        public Action Done;
        public Action<Exception> Handler;
    }

    public class Runner : IDisposable
    {
        public class Args
        {
            public string ThreadName;
            public int IdleDelay;
            public Action IdleAction;
            public Action<Exception> ExceptionHandler;
        }

        private readonly Action<Exception> handler;
        private readonly Queue<Action> queue;
        private readonly Thread thread;
        private readonly Action idle;
        private readonly int delay;
        private volatile bool disposed;

        public Runner(Args args = null)
        {
            args = args ?? new Args();
            this.handler = args.ExceptionHandler;
            this.idle = args.IdleAction;
            this.delay = Math.Max(0, args.IdleDelay);
            if (this.idle == null) this.delay = -1;

            queue = new Queue<Action>();

            thread = new Thread(Loop);
            thread.IsBackground = true;
            thread.Name = args.ThreadName;
            thread.Start();
        }

        public void Dispose(Action action)
        {
            Run(() => { disposed = true; action(); });
            thread.Join();
        }

        public void Dispose()
        {
            Run(() => { disposed = true; });
            thread.Join();
        }

        public void Run(Action action)
        {
            lock (queue)
            {
                queue.Enqueue(action);
                Monitor.Pulse(queue);
            }
        }

        public void Run(Action action, Action<Exception> handler, Action done = null)
        {
            lock (queue)
            {
                queue.Enqueue(() =>
                {
                    Tools.Try(() =>
                    {
                        action();
                        done?.Invoke();
                    }, handler ?? this.handler);
                });
                Monitor.Pulse(queue);
            }
        }

        public void Run(Action action, Callback callback)
        {
            if (callback == null) Run(action);
            else Run(action, callback.Handler, callback.Done);
        }

        private void Loop()
        {
            while (!disposed)
            {
                var action = idle;

                lock (queue)
                {
                    if (queue.Count == 0)
                    {
                        Monitor.Wait(queue, delay);
                    }
                    if (queue.Count > 0)
                    {
                        action = queue.Dequeue();
                    }
                }

                if (action != null)
                {
                    Tools.Try(action, handler);
                }
            }
        }
    }
}