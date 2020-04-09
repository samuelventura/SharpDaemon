set INNO=C:\Program Files (x86)\Inno Setup 5

cd %~dp0
dotnet publish Daemon.StaticWebServer -c Release
dotnet publish SharpDaemon.Service -c Release
"%INNO%\iscc.exe" Installer.iss
