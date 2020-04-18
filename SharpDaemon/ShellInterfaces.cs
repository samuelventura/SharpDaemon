using System;
using System.Collections.Generic;

namespace SharpDaemon
{
    public interface IStream : IWriteLine, IHandleException, IReadLine, ITryReadLine { }

    public interface IShell
    {
        void Execute(IStream stream, params string[] tokens);
    }

    public class ShellFactory
    {
        private readonly List<IShell> shells = new List<IShell>();

        public void Add(IShell shell) => shells.Add(shell);

        public Shell Create()
        {
            return new Shell(new List<IShell>(shells));
        }
    }
}
