using System;

namespace SharpDaemon.Server
{
    public partial class Controller : IScriptable
    {
        public void Execute(Output output, params string[] tokens)
        {
            if (tokens[0] == "daemon")
            {
                var named = new NamedOutput("CONTROLLER", output);

                if ((tokens.Length == 4 || tokens.Length == 5) && tokens[1] == "install")
                {
                    Execute(named, () => ExecuteInstall(named, tokens));
                }
                if (tokens.Length == 3 && tokens[1] == "uninstall")
                {
                    Execute(named, () => ExecuteUninstall(named, tokens));
                }
                if (tokens.Length == 2 && tokens[1] == "status")
                {
                    Execute(named, () => ExecuteStatus(named, tokens));
                }
            }
        }

        private void ExecuteStatus(NamedOutput named, params string[] tokens)
        {
            named.Output("Id|Pid|Name|Status");
            foreach (var rt in daemons.Values)
            {
                named.Output(rt.FullInfo());
            }
            named.Output("{0} daemon(s)", daemons.Count);
        }

        private void ExecuteUninstall(NamedOutput named, params string[] tokens)
        {
            var id = tokens[2];

            if (daemons.ContainsKey(id))
            {
                Tools.Try(daemons[id].Dispose);
                daemons.Remove(id);
                named.Output("Daemon {0} removed", id);
            }
        }

        private void ExecuteInstall(NamedOutput named, params string[] tokens)
        {
            var id = tokens[2];

            if (daemons.ContainsKey(id))
            {
                Tools.Try(daemons[id].Dispose);
                daemons.Remove(id);
                named.Output("Daemon {0} removed", id);
            }

            var dto = new DaemonDto
            {
                Id = tokens[2],
                Path = tokens[3],
                Args = tokens.Length > 4 ? tokens[4] : string.Empty,
            };
            named.Output("Daemon {0} starting {1}|{2}...", dto.Id, dto.Path, dto.Args);
            var rt = new DaemonRT(dto, root, output, handler);
            daemons[id] = rt;
            rt.Run(() => Restart(rt));
            named.Output("Daemon {0} started {1}", rt.Id, rt.ProcessInfo());
        }

        private void Execute(NamedOutput named, Action action) => runner.Run(action, named.OnException);
    }
}
