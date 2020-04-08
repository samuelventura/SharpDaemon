using System;

namespace SharpDaemon
{
    public interface IFormat
    {
        string Format(string format, params object[] args);
    }

    public interface IReadLine
    {
        string ReadLine();
    }

    public interface ITryReadLine
    {
        string TryReadLine(out bool eof);
    }

    public interface IWriteLine
    {
        void WriteLine(string format, params object[] args);
    }
}