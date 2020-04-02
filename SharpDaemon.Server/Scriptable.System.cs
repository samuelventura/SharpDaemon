using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Net.NetworkInformation;

namespace SharpDaemon.Server
{
    public class SystemScriptable : IScriptable
    {
        public void Execute(Output output, params string[] tokens)
        {
            if (tokens[0] == "system")
            {
                if (tokens.Length == 2 && tokens[1] == "counts")
                {
                    ExecuteCounts(output, tokens);
                }
                if (tokens.Length == 2 && tokens[1] == "environment")
                {
                    ExecuteEnvironment(output, tokens);
                }
                if (tokens.Length == 2 && tokens[1] == "network")
                {
                    ExecuteNetwork(output, tokens);
                }
                if (tokens.Length == 2 && tokens[1] == "disk")
                {
                    ExecuteDisk(output, tokens);
                }
                if (tokens.Length == 2 && tokens[1] == "threads")
                {
                    ExecuteThreads(output, tokens);
                }
                if (tokens.Length == 3 && tokens[1] == "list")
                {
                    ExecuteList(output, tokens);
                }
            }
            if (tokens[0] == "help")
            {
                output.WriteLine("system environment");
                output.WriteLine("system network");
                output.WriteLine("system disk");
                output.WriteLine("system threads");
                output.WriteLine("system counts");
                output.WriteLine("system list <path>");
            }
        }

        private void ExecuteList(Output output, params string[] tokens)
        {
            var path = tokens[2];
            var root = Path.GetFullPath(path);
            var total = 0;
            foreach (var file in Directory.GetDirectories(path))
            {
                total++; //remove final \ as well
                output.WriteLine("{0}", file.Substring(root.Length + 1));
            }
            foreach (var file in Directory.GetFiles(path))
            {
                total++; //remove final \ as well
                var furi = new Uri(file);
                output.WriteLine("{0}", file.Substring(root.Length + 1));
            }
            output.WriteLine("{0} total files", total);
        }

        private void ExecuteCounts(Output output, params string[] tokens)
        {
            var total = 0;
            foreach (var pair in Counter.State())
            {
                total += pair.Value;
                output.WriteLine("Count for {0} = {1}", pair.Key, pair.Value);
            }
            output.WriteLine("{0} total counts", total);
            output.WriteLine("{0} total undisposed", Disposable.Undisposed);
        }

        private void ExecuteEnvironment(Output output, params string[] tokens)
        {
            output.WriteLine("OSVersion {0}", Environment.OSVersion);
            output.WriteLine("ProcessorCount {0}", Environment.ProcessorCount);
            output.WriteLine("UserDomainName {0}", Environment.UserDomainName);
            output.WriteLine("UserName {0}", Environment.UserName);
            output.WriteLine("UserInteractive {0}", Environment.UserInteractive);
            output.WriteLine("CurrentDirectory {0}", Environment.CurrentDirectory);
            output.WriteLine("SpecialFolder.UserProfile {0}", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        }

        private void ExecuteNetwork(Output output, params string[] tokens)
        {
            var adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var adapter in adapters)
            {
                var properties = adapter.GetIPProperties();
                output.WriteLine(adapter.Description);
                foreach (var gw in properties.GatewayAddresses) output.WriteLine(" GW {0}", gw.Address);
                foreach (var addr in properties.UnicastAddresses) output.WriteLine(" IP {0}", addr.Address);
            }
        }

        private void ExecuteDisk(Output output, params string[] tokens)
        {
            var drives = DriveInfo.GetDrives();
            foreach (var drive in drives)
            {
                output.WriteLine(drive.Name);
                output.WriteLine(" Type {0}", drive.DriveType);
                output.WriteLine(" IsReady {0}", drive.IsReady);
                output.WriteLine(" RootDirectory {0}", drive.RootDirectory);
                if (drive.IsReady)
                {
                    output.WriteLine(" VolumeLabel {0}", drive.VolumeLabel);
                    output.WriteLine(" TotalSize {0}", drive.TotalSize);
                    output.WriteLine(" TotalFreeSpace {0}", drive.TotalFreeSpace);
                }
            }
        }

        private void ExecuteThreads(Output output, params string[] tokens)
        {
            var list = new List<ProcessThread>();
            var process = Process.GetCurrentProcess();
            output.WriteLine("Current process {0} {1}", process.ProcessName, process.Id);
            foreach (var t in process.Threads) list.Add(t as ProcessThread);
            output.WriteLine("Id|State", list.Count);
            foreach (var thread in list)
            {
                output.WriteLine("{0}|{1}", thread.Id, thread.ThreadState);
            }
            output.WriteLine("{0} total", list.Count);
        }
    }
}