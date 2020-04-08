using System;
using System.Text;

namespace SharpDaemon
{
    public static class Environ
    {
        public static readonly Encoding Encode = Encoding.UTF8;
        public const char NewLine = '\n';
        public const int EndOfFile = -1;
        public const int InfiniteTo = -1;
    }
}