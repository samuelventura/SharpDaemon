using System;

namespace SharpDaemon.Server
{
    public partial class Listener : IShell
    {
        public void Execute(Shell.IO io, params string[] tokens)
        {
            if (tokens[0] == "client")
            {
                if (tokens.Length == 2 && tokens[1] == "list")
                {
                    Execute(io, () => ExecuteList(io, tokens));
                }
                if (tokens.Length == 3 && tokens[1] == "kill")
                {
                    Execute(io, () => ExecuteKill(io, tokens));
                }
            }
            if (tokens[0] == "help")
            {
                io.WriteLine("client list");
                io.WriteLine("client kill <endpoint>");
            }
        }

        private void ExecuteList(IOutput io, params string[] tokens)
        {
            io.WriteLine("Endpoint|Start|Idle");
            foreach (var rt in clients.Values)
            {
                io.WriteLine(rt.Info("Endpoint|Start|Idle"));
            }
            io.WriteLine("{0} client(s) connected", clients.Count);
        }

        private void ExecuteKill(IOutput io, params string[] tokens)
        {
            var id = tokens[2];
            clients.TryGetValue(id, out var rt);
            Tools.Assert(rt != null, "Client {0} not found", id);
            rt.Dispose();
            io.WriteLine("Client {0} killed", id);
        }

        private void Execute(IOutput io, Action action) => register.Run(action, io.OnException);
    }
}