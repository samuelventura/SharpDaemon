using System;
using System.Threading;
using System.Collections.Generic;

namespace SharpDaemon
{
    public class LockedQueue<T>
    {
        private readonly Queue<T> queue = new Queue<T>();

        public void Push(T t)
        {
            lock (queue)
            {
                queue.Enqueue(t);
                Monitor.Pulse(queue);
            }
        }

        public T Pop(int toms, T d)
        {
            lock (queue)
            {
                if (queue.Count == 0)
                {
                    Monitor.Wait(queue, toms);
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
