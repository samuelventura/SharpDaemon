using System;
using System.Threading;

namespace SharpDaemon
{
    public class Runner : Disposable
    {
        public class Command
        {
            public bool Quit;
            public Action Action;
            public Action<Exception> Handler;
        }

        public class Args
        {
            public string ThreadName;
            public int IdleMsDelay;
            public Action IdleAction;
            public Action<Exception> ExceptionHandler;
        }

        private readonly Action<Exception> handler;
        private readonly LockedQueue<Command> queue;
        private readonly Thread thread;
        private readonly Action idle;
        private readonly int delay;
        private volatile bool quit;

        public Runner(Args args = null)
        {
            args = args ?? new Args();

            this.handler = args.ExceptionHandler;
            this.delay = Math.Max(0, args.IdleMsDelay);
            this.idle = args.IdleAction;

            if (idle == null) this.delay = -1;

            queue = new LockedQueue<Command>();

            thread = new Thread(Loop);
            thread.IsBackground = true;
            thread.Name = args.ThreadName;
            thread.Start();
        }

        public void Dispose(Action action)
        {
            Run(new Command { Action = action, Quit = true });
            Dispose(); //notify base and counts
        }

        protected override void Dispose(bool disposed)
        {
            Run(new Command { Quit = true });
            thread.Join();
        }

        public void Run(Action action, Action<Exception> handler)
        {
            Run(new Command { Action = action, Handler = handler });
        }

        public void Run(Action action)
        {
            Run(new Command { Action = action });
        }

        private void Run(Command command)
        {
            //Ideally no push after disposing to avoid holding references to objects
            //that wont be executed. The trick is to discard runner references right
            //after disposing to make all available to GC.
            queue.Push(command);
        }

        private void Loop()
        {
            //provide a non throwing handler to avoid using try here
            while (!quit) ExceptionTools.Try(Process);
        }

        private void Process()
        {
            var command = queue.Pop(delay, new Command { Action = idle });

            if (command.Quit) quit = true;
            if (command.Handler == null) command.Handler = handler;
            if (command.Action != null)
            {
                try
                {
                    command.Action();
                }
                catch (Exception ex)
                {
                    command.Handler?.Invoke(ex);
                }
            }
        }
    }
}