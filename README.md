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
#restart delay defaults to 5000ms
#tcp port defaults to 22333
#ip defaults to 0.0.0.0
dotnet run -p SharpDaemon.Server -- port=12333
#install relative to downloads
#quote command tokens containing spaces with `
daemon uninstall sample
daemon install http://127.0.0.1:9999/sample.zip
daemon install sample ..\..\SharpDaemon.Test.Daemon.exe DelayMs=5000
```

## TODO

- [ ] Improve documentation and samples
- [ ] Provide working URLs in samples above
- [ ] Implement secure connection


