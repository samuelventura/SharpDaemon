netsh advfirewall firewall delete rule name="DaemonManager.Service"
netsh advfirewall firewall delete rule name="DaemonManager.SHELL"
net stop "Daemon Manager"
sc delete "Daemon Manager"
