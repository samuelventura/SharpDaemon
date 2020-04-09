using System;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
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

        public static TcpClient ConnectWithTimeout(string ip, int port, int timeout)
        {
            var client = new TcpClient();
            var result = client.BeginConnect(ip, port, null, null);
            if (!result.AsyncWaitHandle.WaitOne(timeout, true))
            {
                ExceptionTools.Try(client.Close);
                throw ExceptionTools.Make("Timeout connecting to {0}:{1}", ip, port);
            }
            client.EndConnect(result);
            return client;
        }

        public static SslStream SslWithTimeout(TcpClient client, int timeout)
        {
            var endpoint = client.Client.RemoteEndPoint;
            var stream = new SslStream(client.GetStream(), false, AcceptAnyCertificate);
            var result = stream.BeginAuthenticateAsClient(string.Empty, null, null);
            if (!result.AsyncWaitHandle.WaitOne(timeout, true))
            {
                ExceptionTools.Try(stream.Dispose);
                ExceptionTools.Try(client.Close);
                throw ExceptionTools.Make("Timeout authenticating to {0}", endpoint);
            }
            return stream;
        }

        public static bool AcceptAnyCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
    }
}
