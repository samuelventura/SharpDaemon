# SharpDaemon

Windows Daemon Manager

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
#cross platform test cases
dotnet test SharpDaemon.Test
#console output for test cases
dotnet test SharpDaemon.Test -v n
#run with specific framework
dotnet run --project SharpDaemon --framework net40
dotnet publish SharpDaemon -c Release --framework net40
```

## TODO

- [ ] Improve documentation and samples
