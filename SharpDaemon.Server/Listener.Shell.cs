using System;

namespace SharpDaemon.Server
{
    public partial class Listener : IShell
    {
        public void Execute(IStream stream, params string[] tokens)
        {
            if (tokens[0] == "client")
            {
                if (tokens.Length == 2 && tokens[1] == "list")
                {
                    Execute(stream, () => ExecuteList(stream, tokens));
                }
                if (tokens.Length == 3 && tokens[1] == "kill")
                {
                    Execute(stream, () => ExecuteKill(stream, tokens));
                }
            }
            if (tokens[0] == "help")
            {
                stream.WriteLine("client list");
                stream.WriteLine("client kill <endpoint>");
            }
        }

        private void ExecuteList(IOutput output, params string[] tokens)
        {
            output.WriteLine("Endpoint|Start|Idle");
            foreach (var rt in clients.Values)
            {
                output.WriteLine(rt.Info("Endpoint|Start|Idle"));
            }
            output.WriteLine("{0} client(s) connected", clients.Count);
        }

        private void ExecuteKill(IOutput output, params string[] tokens)
        {
            var id = tokens[2];
            clients.TryGetValue(id, out var rt);
            AssertTools.True(rt != null, "Client {0} not found", id);
            rt.Dispose();
            output.WriteLine("Client {0} killed", id);
        }

        private void Execute(IOutput output, Action action) => register.Run(action, output.HandleException);
    }
}