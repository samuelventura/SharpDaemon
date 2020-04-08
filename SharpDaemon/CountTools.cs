using System;
using System.Collections.Generic;

namespace SharpDaemon
{
    public static class Counter
    {
        //292434 years if ++ each usec
        private static readonly object locker = new object();
        private static readonly Dictionary<Type, long> counts = new Dictionary<Type, long>();

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

        public static Dictionary<Type, long> State()
        {
            lock (locker) return new Dictionary<Type, long>(counts);
        }

        public static long Total()
        {
            var state = State();
            var total = 0L;
            foreach (var c in state.Values) total += c;
            return total;
        }
    }
}
