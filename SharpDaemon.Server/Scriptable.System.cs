using System;
using System.IO;
using System.Net.NetworkInformation;

namespace SharpDaemon.Server
{
    public class SystemScriptable : IScriptable
    {
        public void Execute(Output output, params string[] tokens)
        {
            if (tokens[0] == "system")
            {
                var named = new NamedOutput("SYSTEM", output);

                if (tokens.Length == 2 && tokens[1] == "counts")
                {
                    ExecuteCounts(named, tokens);
                }
                if (tokens.Length == 2 && tokens[1] == "environment")
                {
                    ExecuteEnvironment(named, tokens);
                }
                if (tokens.Length == 2 && tokens[1] == "network")
                {
                    ExecuteNetwork(named, tokens);
                }
                if (tokens.Length == 2 && tokens[1] == "disk")
                {
                    ExecuteDisk(named, tokens);
                }
            }
        }

        private void ExecuteCounts(NamedOutput named, params string[] tokens)
        {
            GC.Collect();
            foreach (var pair in Counter.State())
            {
                named.Output("Count for {0} = {1}", pair.Key, pair.Value);
            }
        }

        private void ExecuteEnvironment(NamedOutput named, params string[] tokens)
        {
            named.Output("OSVersion {0}", Environment.OSVersion);
            named.Output("ProcessorCount {0}", Environment.ProcessorCount);
            named.Output("UserDomainName {0}", Environment.UserDomainName);
            named.Output("UserName {0}", Environment.UserName);
            named.Output("UserInteractive {0}", Environment.UserInteractive);
            named.Output("CurrentDirectory {0}", Environment.CurrentDirectory);
            named.Output("SpecialFolder.UserProfile {0}", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        }

        private void ExecuteNetwork(NamedOutput named, params string[] tokens)
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

        private void ExecuteDisk(NamedOutput named, params string[] tokens)
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