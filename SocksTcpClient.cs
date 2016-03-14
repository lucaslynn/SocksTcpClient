using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.IO;

namespace SocksTcpClient
{
    public delegate void ClientStatusChangedEvent(object sender, string status);

    class SocksTcpClient : TcpClient
    {
        public event ClientStatusChangedEvent StatusChanged;
        private enum SocksType
        {
            Socks5 = 0x05
        }

        private enum Authentication
        {
            None = 0x00,
            GSSApi = 0x01,
            Credentials = 0x02,
            NoAcceptableMethods = 0xFF
        }

        private string[] errorMsgs = new string[]
        {
            "Successfully connected to remote host!",
            "General SOCKS server failure!",
            "Connection not allowed by ruleset!",
            "Network unreachable!",
            "Host unreachable!",
            "Connection refused!",
            "TTL expired!",
            "Command not supported!",
            "Address type not supported!"
        };

        public SocksTcpClient() { }

        public void Connect(string proxyHost, int proxyPort, string targetHost, int targetPort)
        {
            IPAddress proxyHostAddress;
            if (!IPAddress.TryParse(proxyHost, out proxyHostAddress))
                proxyHostAddress = Dns.GetHostAddresses(proxyHost)[0];
            IPEndPoint proxyEndPoint = new IPEndPoint(proxyHostAddress, proxyPort);

            Socket socket = base.Client;
            socket.Connect(proxyEndPoint);

            socket = Negotiate(socket);
            if (socket == null)
                return;

            socket = Handshake(socket, targetHost, targetPort);
        }

        private Socket Negotiate(Socket socket)
        {
            byte[] requestBytes = new byte[] { (byte)SocksType.Socks5, 0x03, (byte)Authentication.None, (byte)Authentication.GSSApi, (byte)Authentication.NoAcceptableMethods };
            byte[] responseBytes = new byte[2];

            socket.Send(requestBytes, requestBytes.Length, SocketFlags.None);
            int bytesReceived = socket.Receive(responseBytes, 2, SocketFlags.None);

            if (bytesReceived != 2)
            {
                OnStatusChanged(socket, "Failed to negotiate with the proxy!");
                return null;
            }

            if (responseBytes[1] == (byte)Authentication.None)
            {
                OnStatusChanged(socket, "Successfully negotiated with the proxy!");
                return socket;
            }

            return null;
        }

        private Socket Handshake(Socket socket, string targetHost, int targetPort)
        {
            int i = 0;
            byte[] addressBytes = Encoding.Default.GetBytes(targetHost);
            byte[] portBytes = BitConverter.GetBytes(targetPort);
            byte[] requestBytes = new byte[addressBytes.Length + portBytes.Length + 4];
            byte[] responseBytes = new byte[2];
            requestBytes[i++] = (byte)SocksType.Socks5;
            requestBytes[i++] = 0x01;
            requestBytes[i++] = 0x00;
            requestBytes[i++] = 0x01;

            addressBytes.CopyTo(requestBytes, i);
            i += addressBytes.Length;

            for (int b = portBytes.Length - 1; b >= 0; b--)
                requestBytes[i++] = portBytes[b];

            socket.Send(requestBytes, i, SocketFlags.None);
            int receivedBytes = socket.Receive(responseBytes, 2, SocketFlags.None);

            if (receivedBytes != 2)
            {
                OnStatusChanged(socket, "Bad proxy!");
                return null;
            }

            OnStatusChanged(socket, errorMsgs[responseBytes[1]]);

            if (responseBytes[1] == 0x00)
            {
                return socket;
            }

            return null;
        }

        private void OnStatusChanged(object sender, string status)
        {
            ClientStatusChangedEvent handler = StatusChanged;
            if (handler != null)
                handler(sender, status);
        }
    }
}