using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace SharpDaemon
{
    public static class SocketTools
    {
        //https://stackoverflow.com/questions/35280597/net-tcplistener-stop-method-does-not-stop-the-listener-when-there-is-a-child-pr
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetHandleInformation(IntPtr hObject, uint dwMask, uint dwFlags);
        private const uint HANDLE_FLAG_INHERIT = 1;

        public static void MakeNotInheritable(this TcpListener tcpListener)
        {
            var handle = tcpListener.Server.Handle;
            SetHandleInformation(handle, HANDLE_FLAG_INHERIT, 0);
        }
    }
}
