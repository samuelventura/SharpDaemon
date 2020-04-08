using System;
using System.IO;
using System.Management;
using System.Diagnostics;
using System.Collections.Generic;
using System.Net.NetworkInformation;

namespace SharpDaemon.Server
{
    public class SystemScriptable : IShell
    {
        public void Execute(IStream stream, params string[] tokens)
        {
            if (tokens[0] == "system")
            {
                if (tokens.Length == 2 && tokens[1] == "counts")
                {
                    ExecuteCounts(stream, tokens);
                }
                if (tokens.Length == 2 && tokens[1] == "environment")
                {
                    ExecuteEnvironment(stream, tokens);
                }
                if (tokens.Length == 2 && tokens[1] == "network")
                {
                    ExecuteNetwork(stream, tokens);
                }
                if (tokens.Length == 2 && tokens[1] == "disk")
                {
                    ExecuteDisk(stream, tokens);
                }
                if (tokens.Length == 2 && tokens[1] == "threads")
                {
                    ExecuteThreads(stream, tokens);
                }
                if (tokens.Length == 2 && tokens[1] == "children")
                {
                    ExecuteChildren(stream, tokens);
                }
                if (tokens.Length == 3 && tokens[1] == "list")
                {
                    ExecuteList(stream, tokens);
                }
            }
            if (tokens[0] == "help")
            {
                stream.WriteLine("system environment");
                stream.WriteLine("system network");
                stream.WriteLine("system disk");
                stream.WriteLine("system threads");
                stream.WriteLine("system children");
                stream.WriteLine("system counts");
                stream.WriteLine("system list <folder-absolute-path>");
            }
        }

        private void ExecuteList(IOutput output, params string[] tokens)
        {
            var path = tokens[2];
            var root = Path.GetFullPath(path);
            var total = 0;
            foreach (var file in Directory.GetDirectories(root))
            {
                total++; //remove final \ as well
                output.WriteLine("{0}", file.Substring(root.Length + 1));
            }
            output.WriteLine("{0} total directories", total);
            total = 0;
            foreach (var file in Directory.GetFiles(root))
            {
                total++; //remove final \ as well
                var furi = new Uri(file);
                output.WriteLine("{0}", file.Substring(root.Length + 1));
            }
            output.WriteLine("{0} total files", total);
        }

        private void ExecuteCounts(IOutput output, params string[] tokens)
        {
            var total = 0L;
            foreach (var pair in Counter.State())
            {
                total += pair.Value;
                output.WriteLine("Count for {0} = {1}", pair.Key, pair.Value);
            }
            output.WriteLine("{0} total counts", total);
            output.WriteLine("{0} total undisposed", Disposable.Undisposed);
        }

        private void ExecuteEnvironment(IOutput output, params string[] tokens)
        {
            output.WriteLine("OSVersion={0}", Environment.OSVersion);
            output.WriteLine("ProcessorCount={0}", Environment.ProcessorCount);
            output.WriteLine("UserDomainName={0}", Environment.UserDomainName);
            output.WriteLine("UserName={0}", Environment.UserName);
            output.WriteLine("UserInteractive={0}", Environment.UserInteractive);
            output.WriteLine("CurrentDirectory={0}", Environment.CurrentDirectory);
            output.WriteLine("SpecialFolder.UserProfile={0}", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        }

        private void ExecuteNetwork(IOutput output, params string[] tokens)
        {
            var adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var adapter in adapters)
            {
                var properties = adapter.GetIPProperties();
                output.WriteLine(adapter.Description);
                foreach (var gw in properties.GatewayAddresses) output.WriteLine(" GW {0}", gw.Address);
                foreach (var addr in properties.UnicastAddresses) output.WriteLine(" IP {0}", addr.Address);
            }
            output.WriteLine("{0} total", adapters.Length);
        }

        private void ExecuteDisk(IOutput output, params string[] tokens)
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
            output.WriteLine("{0} total", drives.Length);
        }

        private void ExecuteThreads(IOutput output, params string[] tokens)
        {
            var list = new List<ProcessThread>();
            var process = Process.GetCurrentProcess();
            output.WriteLine("Current process id {0}", process.Id);
            foreach (var t in process.Threads) list.Add(t as ProcessThread);
            output.WriteLine("Id|State", list.Count);
            foreach (var thread in list)
            {
                output.WriteLine("{0}|{1}", thread.Id, thread.ThreadState);
            }
            output.WriteLine("{0} total", list.Count);
        }

        private void ExecuteChildren(IOutput output, params string[] tokens)
        {
            var count = 0;
            var process = Process.GetCurrentProcess();
            output.WriteLine("Current process id {0}", process.Id);
            var mos = new ManagementObjectSearcher($"Select * From Win32_Process Where ParentProcessID={process.Id}");
            foreach (var mo in mos.Get())
            {
                count++;
                output.WriteLine(" Child PID {0}", mo["ProcessID"]);
            }
            output.WriteLine("{0} total", count);
        }
    }
}