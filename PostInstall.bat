rem netsh advfirewall firewall show rule name="DaemonManager.Service"
netsh advfirewall firewall add rule name="DaemonManager.Service" dir=in action=allow program="%~dp0SharpDaemon.Service.exe" enable=yes
rem netsh advfirewall firewall show rule name="DaemonManager.Shell"
netsh advfirewall firewall add rule name="DaemonManager.Shell" dir=in action=allow protocol=TCP localport=22333
sc create "Daemon Manager" binpath="%~dp0SharpDaemon.Service.exe"
sc description "Daemon Manager" "Provides remote shell for daemon management"
sc config "Daemon Manager" start=auto
net start "Daemon Manager"
