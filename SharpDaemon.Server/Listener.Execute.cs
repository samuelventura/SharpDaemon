using System;

namespace SharpDaemon.Server
{
    public partial class Listener : IScriptable
    {
        public void Execute(Output output, params string[] tokens)
        {
            if (tokens[0] == "client")
            {
                var named = new NamedOutput("LISTENER", output);
                if (tokens.Length == 2 && tokens[1] == "list")
                {
                    Execute(named, () => ExecuteList(output, named, tokens));
                }
            }
        }

        private void ExecuteList(Output output, NamedOutput named, params string[] tokens)
        {
            named.Output("Endpoint|Start|Idle");
            foreach (var rt in clients)
            {
                named.Output(rt.Info());
            }
            named.Output("{0} client(s)", clients.Count);
        }

        private void Execute(NamedOutput named, Action action) => register.Run(action, named.OnException);
    }
}