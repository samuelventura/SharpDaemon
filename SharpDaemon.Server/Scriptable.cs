using System;
using System.IO;
using System.Collections.Generic;
using System.Net.NetworkInformation;

namespace SharpDaemon.Server
{
    public interface IScriptable
    {
        void Execute(string[] tokens, Output output);
    }

    public class SystemScriptable : IScriptable
    {
        public void Execute(string[] tokens, Output output)
        {
            if (tokens[0] == "system")
            {
                var named = new NamedOutput("SYSTEM", output);

                if (tokens.Length == 2 && tokens[1] == "environment")
                {
                    named.Output("OSVersion {0}", Environment.OSVersion);
                    named.Output("ProcessorCount {0}", Environment.ProcessorCount);
                    named.Output("UserDomainName {0}", Environment.UserDomainName);
                    named.Output("UserName {0}", Environment.UserName);
                    named.Output("UserInteractive {0}", Environment.UserInteractive);
                    named.Output("CurrentDirectory {0}", Environment.CurrentDirectory);
                    named.Output("SpecialFolder.UserProfile {0}", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
                }
                if (tokens.Length == 2 && tokens[1] == "network")
                {
                    var adapters = NetworkInterface.GetAllNetworkInterfaces();
                    foreach (var adapter in adapters)
                    {
                        var properties = adapter.GetIPProperties();
                        named.Output(adapter.Description);
                        foreach (var gw in properties.GatewayAddresses) named.Output(" GW {0}", gw.Address);
                        foreach (var addr in properties.UnicastAddresses) named.Output(" IP {0}", addr.Address);
                    }
                }
                if (tokens.Length == 2 && tokens[1] == "disk")
                {
                    var drives = DriveInfo.GetDrives();
                    foreach (var drive in drives)
                    {
                        named.Output(drive.Name);
                        named.Output(" Type {0}", drive.DriveType);
                        named.Output(" IsReady {0}", drive.IsReady);
                        named.Output(" RootDirectory {0}", drive.RootDirectory);
                        if (drive.IsReady)
                        {
                            named.Output(" VolumeLabel {0}", drive.VolumeLabel);
                            named.Output(" TotalSize {0}", drive.TotalSize);
                            named.Output(" TotalFreeSpace {0}", drive.TotalFreeSpace);
                        }
                    }
                }
            }
        }
    }

    public class Shell : IScriptable
    {
        private readonly List<IScriptable> scriptables;

        public Shell(List<IScriptable> scriptables)
        {
            this.scriptables = scriptables;
        }

        public void Execute(string[] tokens, Output output)
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