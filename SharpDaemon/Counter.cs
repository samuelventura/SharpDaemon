using System;
using System.Collections.Generic;

namespace SharpDaemon
{
    public static class Counter
    {
        private static readonly object locker = new object();
        private static readonly Dictionary<Type, int> counts = new Dictionary<Type, int>();

        public static void Plus(object o)
        {
            lock (locker)
            {
                var type = o.GetType();
                counts.TryGetValue(type, out var count);
                counts[type] = ++count;
            }
        }

        public static void Minus(object o)
        {
            lock (locker)
            {
                var type = o.GetType();
                counts.TryGetValue(type, out var count);
                counts[type] = --count;
            }
        }

        public static Dictionary<Type, int> State()
        {
            lock (locker) return new Dictionary<Type, int>(counts);
        }
    }
}
