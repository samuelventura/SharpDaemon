using System;
using System.Collections.Generic;

namespace SharpDaemon.Server
{
    public interface IScriptable
    {
        void Execute(string[] tokens, Output output);
    }

    public class Shell
    {
        private readonly List<IScriptable> scriptables;

        public Shell(List<IScriptable> scriptables)
        {
            this.scriptables = scriptables;
        }

        public void OnLine(string[] tokens, Output output)
        {
            var list = new List<string>(tokens);
            foreach (var script in scriptables)
            {
                Tools.Try(
                    () => script.Execute(list.ToArray(), output),
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