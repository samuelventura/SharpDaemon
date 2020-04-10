# SharpDaemon

Windows Daemon Manager

## Documentation

A daemon is a well behaved console application that:

- Exits on closing of stdin (Console.ReadLine returns null)
- Reports its status by writting it to stdout (Console.WriteLine)
- Installs global unhandled exception catcher to:
  - Dump exception to Exceptions\Exception-TS-ProcName-Pid.txt
  - Write exception message to stdout (Console.WriteLine)
  - Exit with non zero code 

Since many daemon instances can share a single executable the manager will always pass the registered daemon id as the Pid argument for the daemon to self identify (and for proper management of resources, like independent log files, if needed).

Daemons can only be installed into the workspace by:

 - Downloading and uncompressing a zip from a given HTTP URI
 - Issuing the install command specifying:
   - A string identifier (id)
   - The executable path relative to download folder
   - The arguments to pass to the executable after the id

## Development Setup

- Windows 10 Pro 64x (Windows only)
- VS Code (bash terminal from Git4Win)
- Net Core SDK 3.1.201
- net462 supporting Windows 7 SP1 (x86 and x64)
  - [.NET Framework 4.6.2 Download Page](https://dotnet.microsoft.com/download/dotnet-framework/net462)
  - [Offline Installer](https://www.microsoft.com/en-us/download/details.aspx?id=53344)
  - [Developer Pack](https://www.microsoft.com/en-us/download/details.aspx?id=53321)
- GNU Win32
  - Add C:\Program Files (x86)\GnuWin32\bin to PATH
  - [Zip for Windown](http://gnuwin32.sourceforge.net/packages/zip.htm)
  - [OpenSSL for Windows](http://gnuwin32.sourceforge.net/packages/openssl.htm)

## Development CLI

```bash
#nuget packing and publishing
dotnet clean SharpDaemon -c Release
dotnet pack SharpDaemon -c Release
dotnet publish SharpDaemon.Service -c Release
#test cases
dotnet test SharpDaemon.Test
dotnet test SharpDaemon.Test --filter FullyQualifiedName~BasicTest
dotnet test SharpDaemon.Test --filter FullyQualifiedName~RunCmdTest
dotnet test SharpDaemon.Test --filter FullyQualifiedName~WebServerTest
#console output for test cases
dotnet test SharpDaemon.Test -v n
#run (type exit! to exit)
#workspace defaults to Root={EXEDIR}\Root
#restart delay defaults to Delay=5000ms
#tcp port defaults to Port=22333
#ip defaults to IP=0.0.0.0
dotnet run -p SharpDaemon.Server -- Port=12333
#type help for shell commands help
shell>help
shell>exit!
```

### Linux/MacOS Development

- [Mono Devel](https://www.mono-project.com/download/stable/#download-lin)
  - OnmiSharp for VSCode worked after installing it

```bash
dotnet build SharpDaemon -f netstandard2.0
dotnet build SharpDaemon.Server -f netcoreapp3.1
#test cases
dotnet test SharpDaemon.Test -f netcoreapp3.1
dotnet test SharpDaemon.Test -f netcoreapp3.1 --filter FullyQualifiedName~RunCmdTest
dotnet test SharpDaemon.Test -f netcoreapp3.1 --filter FullyQualifiedName~BasicTest
dotnet test SharpDaemon.Test -f netcoreapp3.1 --filter FullyQualifiedName~DaemonLoopExitTest
dotnet test SharpDaemon.Test -f netcoreapp3.1 --filter FullyQualifiedName~ShellRunCmdTest
dotnet test SharpDaemon.Test -f netcoreapp3.1 --filter FullyQualifiedName~ClientRunCmdTest
dotnet test SharpDaemon.Test -f netcoreapp3.1 --filter FullyQualifiedName~ShellLoopExitTest
#run into testing environment
dotnet SharpDaemon.Test/bin/Debug/netcoreapp3.1/SharpDaemon.Server.dll Port=12333 Root=$PWD/SharpDaemon.Test/bin/Debug/netcoreapp3.1/Root
dotnet SharpDaemon.Test/bin/Debug/netcoreapp3.1/Daemon.StaticWebServer.dll EndPoint=127.0.0.1:12334 Root=$PWD/SharpDaemon.Test/bin/Debug/netcoreapp3.1/Root/Web
#run samples
dotnet run -p SharpDaemon.Server -f netcoreapp3.1 -- Port=12333
#point to http://127.0.0.1:8899/.bashrc (localhost not resolved)
dotnet run -p Daemon.StaticWebServer -f netcoreapp3.1 -- Trace=true EndPoint=127.0.0.1:8899 Root=~
```

## TODO

- [ ] Improve documentation and samples
- [ ] Provide useful daemon sample
- [ ] Improve test coverage
- [ ] Support Linux
