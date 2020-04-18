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
        public void Execute(IStream io, params string[] tokens)
        {
            if (tokens[0] == "system")
            {
                if (tokens.Length == 2 && tokens[1] == "counts")
                {
                    ExecuteCounts(io, tokens);
                }
                if (tokens.Length == 2 && tokens[1] == "environment")
                {
                    ExecuteEnvironment(io, tokens);
                }
                if (tokens.Length == 2 && tokens[1] == "network")
                {
                    ExecuteNetwork(io, tokens);
                }
                if (tokens.Length == 2 && tokens[1] == "disk")
                {
                    ExecuteDisk(io, tokens);
                }
                if (tokens.Length == 2 && tokens[1] == "threads")
                {
                    ExecuteThreads(io, tokens);
                }
                if (tokens.Length == 2 && tokens[1] == "children")
                {
                    ExecuteChildren(io, tokens);
                }
                if (tokens.Length == 3 && tokens[1] == "list")
                {
                    ExecuteList(io, tokens);
                }
                if (tokens.Length == 3 && tokens[1] == "password")
                {
                    ExecutePassword(io, tokens);
                }
            }
            if (tokens[0] == "help")
            {
                io.WriteLine("system environment");
                io.WriteLine("system network");
                io.WriteLine("system disk");
                io.WriteLine("system threads");
                io.WriteLine("system children");
                io.WriteLine("system counts");
                io.WriteLine("system list <folder-absolute-path>");
                io.WriteLine("system password <new-password>");
            }
        }

        private void ExecuteList(IStream io, params string[] tokens)
        {
            var path = tokens[2];
            var root = Path.GetFullPath(path);
            var total = 0;
            foreach (var file in Directory.GetDirectories(root))
            {
                total++; //remove final \ as well
                io.WriteLine("{0}", file.Substring(root.Length + 1));
            }
            io.WriteLine("{0} total directories", total);
            total = 0;
            foreach (var file in Directory.GetFiles(root))
            {
                total++; //remove final \ as well
                var furi = new Uri(file);
                io.WriteLine("{0}", file.Substring(root.Length + 1));
            }
            io.WriteLine("{0} total files", total);
        }

        private void ExecutePassword(IStream io, params string[] tokens)
        {
            var password = tokens[2];
            var passfile = ExecutableTools.Relative("Password.txt");
            File.WriteAllText(passfile, password);
            io.WriteLine("Password changed");
        }

        private void ExecuteCounts(IStream io, params string[] tokens)
        {
            var total = 0L;
            foreach (var pair in Counter.State())
            {
                total += pair.Value;
                io.WriteLine("Count for {0} = {1}", pair.Key, pair.Value);
            }
            io.WriteLine("{0} total counts", total);
            io.WriteLine("{0} total undisposed", Disposable.Undisposed);
        }

        private void ExecuteEnvironment(IStream io, params string[] tokens)
        {
            io.WriteLine("OSVersion={0}", Environment.OSVersion);
            io.WriteLine("ProcessorCount={0}", Environment.ProcessorCount);
            io.WriteLine("UserDomainName={0}", Environment.UserDomainName);
            io.WriteLine("UserName={0}", Environment.UserName);
            io.WriteLine("UserInteractive={0}", Environment.UserInteractive);
            io.WriteLine("CurrentDirectory={0}", Environment.CurrentDirectory);
            io.WriteLine("SpecialFolder.UserProfile={0}", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        }

        private void ExecuteNetwork(IStream io, params string[] tokens)
        {
            var adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var adapter in adapters)
            {
                var properties = adapter.GetIPProperties();
                io.WriteLine(adapter.Description);
                foreach (var gw in properties.GatewayAddresses) io.WriteLine(" GW {0}", gw.Address);
                foreach (var addr in properties.UnicastAddresses) io.WriteLine(" IP {0}", addr.Address);
            }
            io.WriteLine("{0} total", adapters.Length);
        }

        private void ExecuteDisk(IStream io, params string[] tokens)
        {
            var drives = DriveInfo.GetDrives();
            foreach (var drive in drives)
            {
                io.WriteLine(drive.Name);
                io.WriteLine(" Type {0}", drive.DriveType);
                io.WriteLine(" IsReady {0}", drive.IsReady);
                io.WriteLine(" RootDirectory {0}", drive.RootDirectory);
                if (drive.IsReady)
                {
                    io.WriteLine(" VolumeLabel {0}", drive.VolumeLabel);
                    io.WriteLine(" TotalSize {0}", drive.TotalSize);
                    io.WriteLine(" TotalFreeSpace {0}", drive.TotalFreeSpace);
                }
            }
            io.WriteLine("{0} total", drives.Length);
        }

        private void ExecuteThreads(IStream io, params string[] tokens)
        {
            var list = new List<ProcessThread>();
            var process = Process.GetCurrentProcess();
            io.WriteLine("Current process id {0}", process.Id);
            foreach (var t in process.Threads) list.Add(t as ProcessThread);
            io.WriteLine("Id|State", list.Count);
            foreach (var thread in list)
            {
                io.WriteLine("{0}|{1}", thread.Id, thread.ThreadState);
            }
            io.WriteLine("{0} total", list.Count);
        }

        private void ExecuteChildren(IStream io, params string[] tokens)
        {
            var count = 0;
            var process = Process.GetCurrentProcess();
            io.WriteLine("Current process id {0}", process.Id);
            var mos = new ManagementObjectSearcher($"Select * From Win32_Process Where ParentProcessID={process.Id}");
            foreach (var mo in mos.Get())
            {
                count++;
                io.WriteLine(" Child PID {0}", mo["ProcessID"]);
            }
            io.WriteLine("{0} total", count);
        }
    }
}