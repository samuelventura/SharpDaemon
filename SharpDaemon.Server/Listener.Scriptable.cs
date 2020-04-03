using System;

namespace SharpDaemon.Server
{
    public partial class Listener : IScriptable
    {
        public void Execute(Output output, params string[] tokens)
        {
            if (tokens[0] == "client")
            {
                if (tokens.Length == 2 && tokens[1] == "list")
                {
                    Execute(output, () => ExecuteList(output, tokens));
                }
                if (tokens.Length == 3 && tokens[1] == "kill")
                {
                    Execute(output, () => ExecuteKill(output, tokens));
                }
            }
            if (tokens[0] == "help")
            {
                output.WriteLine("client list");
                output.WriteLine("client kill <endpoint>");
            }
        }

        private void ExecuteList(Output output, params string[] tokens)
        {
            output.WriteLine("Endpoint|Start|Idle");
            foreach (var rt in clients.Values)
            {
                output.WriteLine(rt.Info("Endpoint|Start|Idle"));
            }
            output.WriteLine("{0} client(s) connected", clients.Count);
        }

        private void ExecuteKill(Output output, params string[] tokens)
        {
            var id = tokens[2];
            clients.TryGetValue(id, out var rt);
            Tools.Assert(rt != null, "Client {0} not found", id);
            rt.Dispose();
            output.WriteLine("Client {0} killed", id);
        }

        private void Execute(Output output, Action action) => register.Run(action, output.OnException);
    }
}