using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;

namespace System.Net.Socks
{
    public delegate void ClientStatusChangedEvent(object sender, string status);

    public class SocksTcpClient
    {
        public static event ClientStatusChangedEvent StatusChanged;

        private enum SocksType
        {
            Socks4 = 0x04,
            Socks5 = 0x05
        }

        private enum Authentication
        {
            None = 0x00,
            GSSApi = 0x01,
            Credentials = 0x02,
            NoAcceptableMethods = 0xFF
        }

        private enum Method
        {
            Connect = 0x01,
            Bind = 0x02,
            UDP = 0x03
        }

        private enum AddressType
        {
            IPv4 = 0x01,
            IPv6 = 0x04
        }

        private static string[] errorMsgs = new string[] {
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

        public static TcpClient Connect(string proxyHost, int proxyPort, string targetHost, int targetPort)
        {
            TcpClient SocksClient = new TcpClient();

            IPAddress proxyHostAddress;
            if (!IPAddress.TryParse(proxyHost, out proxyHostAddress))
                proxyHostAddress = Dns.GetHostAddresses(proxyHost)[0];
            IPEndPoint proxyEndPoint = new IPEndPoint(proxyHostAddress, proxyPort);

            IPAddress targetHostAddress;
            if (!IPAddress.TryParse(targetHost, out targetHostAddress))
                targetHostAddress = Dns.GetHostAddresses(targetHost).Where(ip => ip.AddressFamily == AddressFamily.InterNetwork).FirstOrDefault();

            Socket socket = SocksClient.Client;
            socket.Connect(proxyEndPoint);

            socket = Negotiate(socket, targetHostAddress, targetPort);

            if (socket == null)
                return null;

            return SocksClient;
        }

        private static Socket Negotiate(Socket socket, IPAddress targetHostAddress, int targetPort)
        {
            byte[] requestBytes = new byte[] { (byte)SocksType.Socks5, 0x03, (byte)Authentication.None, (byte)Authentication.GSSApi, (byte)Authentication.NoAcceptableMethods };
            byte[] responseBytes = new byte[2];

            socket.Send(requestBytes, requestBytes.Length, SocketFlags.None);
            int receivedBytes = socket.Receive(responseBytes, 2, SocketFlags.None);

            if (receivedBytes < 2)
            {
                OnStatusChanged(socket, "Bad proxy!");
                return null;
            }

            if (responseBytes[0] == (byte)SocksType.Socks5 && responseBytes[1] == (byte)Authentication.None)
            {
                OnStatusChanged(socket, "Successfully negotiated with the proxy!");

                socket = Handshake(socket, targetHostAddress, targetPort);

                return socket;
            }
            else
            {
                OnStatusChanged(socket, "Failed to negotiate with the proxy!");
                return null;
            }
        }

        private static Socket Handshake(Socket socket, IPAddress targetHostAddress, int targetPort)
        {
            byte[] addressBytes = targetHostAddress.GetAddressBytes();
            byte[] portBytes = new byte[2] { (byte)(targetPort / 256), (byte)(targetPort % 256) };
            byte[] requestBytes = new byte[10];
            byte[] responseBytes = new byte[2];
            requestBytes[0] = (byte)SocksType.Socks5;
            requestBytes[1] = (byte)Method.Connect;
            requestBytes[2] = 0x00;

            if (targetHostAddress.AddressFamily == AddressFamily.InterNetwork)
                requestBytes[3] = (byte)AddressType.IPv4;
            else if (targetHostAddress.AddressFamily == AddressFamily.InterNetworkV6)
                requestBytes[3] = (byte)AddressType.IPv6;

            addressBytes.CopyTo(requestBytes, 4);
            portBytes.CopyTo(requestBytes, 8);

            socket.Send(requestBytes, 10, SocketFlags.None);
            socket.Receive(responseBytes, SocketFlags.None);

            OnStatusChanged(socket, errorMsgs[responseBytes[1]]);

            if (responseBytes[1] != 0x00)
                return null;

            return socket;
        }

        private static void OnStatusChanged(object sender, string status)
        {
            ClientStatusChangedEvent handler = StatusChanged;
            if (handler != null)
                handler(sender, status);
        }
    }
}