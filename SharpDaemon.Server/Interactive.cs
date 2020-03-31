using System;
using System.Collections.Generic;

namespace SharpDaemon.Server
{
    public interface ShellCommand
    {
        void OnLine(string line, Output output);
    }

    public class Shell
    {
        private readonly List<ShellCommand> commands;

        public Shell(List<ShellCommand> commands)
        {
            this.commands = commands;
        }

        public void OnLine(string line, Output output)
        {
            foreach (var cmd in commands)
            {
                Tools.Try(() => cmd.OnLine(line, output), (ex) => output.Output(ex.ToString()));
            }
        }
    }

    public class ShellFactory
    {
        private readonly List<ShellCommand> commands;

        public ShellFactory()
        {
            commands = new List<ShellCommand>();
        }

        public void Add(ShellCommand shell)
        {
            commands.Add(shell);
        }

        public Shell Create()
        {
            return new Shell(new List<ShellCommand>(commands));
        }
    }
}