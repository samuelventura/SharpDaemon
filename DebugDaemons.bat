rem Generate Daemon Zips for Web Exporting During Development and Testing
rem Requires http://gnuwin32.sourceforge.net/packages/zip.htm
set PATH="C:\Program Files (x86)\GnuWin32\bin";%PATH%
rem echo %PATH%
cd %~dp0
dotnet publish Daemon.StaticWebServer
mkdir Output
zip -FSrj Output\Daemon.StaticWebServer.zip Daemon.StaticWebServer\bin\Debug\net452\publish
Daemon.StaticWebServer\bin\Debug\net452\publish\Daemon.StaticWebServer Id=test EndPoint=127.0.0.1:12334 Root="%~dp0Output"
