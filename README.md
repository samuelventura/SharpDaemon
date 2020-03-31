# SharpDaemon

Windows Remote Daemon Manager

## Documentation

No documentation yet.

## Development Setup

- Windows 10 Pro 64x (Windows only)
- VS Code (bash terminal from Git4Win)
- Net Core SDK 3.1.201

## Development CLI

```bash
#nuget packing and publishing
dotnet clean SharpDaemon -c Release
dotnet pack SharpDaemon -c Release
dotnet publish SharpDaemon.Service -c Release
#cross platform test cases
dotnet test SharpDaemon.Test
#console output for test cases
dotnet test SharpDaemon.Test -v n
#run (ctrl+c to exit)
dotnet run -p SharpDaemon.Server -- port=5667
dotnet run -p SharpDaemon.Server
```

## TODO

- [ ] Improve documentation and samples
- [ ] Implement download and install
- [ ] Implement secure connection

