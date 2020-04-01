using System;
using System.Threading;
using System.Collections.Generic;

namespace SharpDaemon
{
    public class LockedQueue<T>
    {
        private readonly object locker = new object();
        private readonly Queue<T> queue = new Queue<T>();

        public void Push(T t)
        {
            lock (locker)
            {
                queue.Enqueue(t);

                Monitor.Pulse(locker);
            }
        }

        public T Pop(int toms, T d)
        {
            lock (locker)
            {
                if (queue.Count == 0)
                {
                    Monitor.Wait(locker, toms);
                }
                if (queue.Count > 0)
                {
                    return queue.Dequeue();
                }
            }
            return d;
        }
    }
}
