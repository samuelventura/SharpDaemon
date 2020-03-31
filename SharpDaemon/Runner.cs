﻿using System;
using System.Collections.Generic;
using System.Threading;

namespace SharpDaemon
{
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
        private readonly LockedQueue<Action> queue;
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

            queue = new LockedQueue<Action>();

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
            queue.Push(action);
        }

        public void Run(Action action, Action<Exception> handler)
        {
            queue.Push(() => Tools.Try(action, handler));
        }

        private void Loop()
        {
            while (!disposed)
            {
                var action = queue.Pop(delay, idle);

                if (action != null)
                {
                    Tools.Try(action, handler);
                }
            }
        }
    }
}