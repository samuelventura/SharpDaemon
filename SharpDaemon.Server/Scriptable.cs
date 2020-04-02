using System;
using System.Collections.Generic;

namespace SharpDaemon.Server
{
    public interface IScriptable
    {
        void Execute(Output output, params string[] tokens);
    }

    public class Shell : IScriptable
    {
        private readonly List<IScriptable> scriptables;

        public Shell(List<IScriptable> scriptables)
        {
            this.scriptables = scriptables;
        }

        public void Execute(Output output, params string[] tokens)
        {
            var list = new List<string>(tokens);
            foreach (var script in scriptables)
            {
                Tools.Try(
                    () => script.Execute(output, list.ToArray()),
                    (ex) => output.Output(ex.ToString())
                );
            }
        }
    }

    public class ShellFactory
    {
        private readonly List<IScriptable> commands;

        public ShellFactory()
        {
            commands = new List<IScriptable>();
        }

        public void Add(IScriptable shell)
        {
            commands.Add(shell);
        }

        public Shell Create()
        {
            return new Shell(new List<IScriptable>(commands));
        }
    }
}