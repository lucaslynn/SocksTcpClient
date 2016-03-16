using System.Linq;
using System.Net.Sockets;
using System.Net;

namespace SocksTcpClient
{
    public delegate void ClientStatusChangedEvent(object sender, string status);
    
    class SocksTcpClient
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

        private static string[] errorMsgs = new string[]
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

            socket = Negotiate(socket);
            if (socket == null)
                return null;

            socket = Handshake(socket, targetHostAddress, targetPort);

            return SocksClient;
        }
        
        private static Socket Negotiate(Socket socket)
        {
            byte[] requestBytes = new byte[] { (byte)SocksType.Socks5, 0x03, (byte)Authentication.None, (byte)Authentication.GSSApi, (byte)Authentication.NoAcceptableMethods };
            byte[] responseBytes = new byte[2];

            socket.Send(requestBytes, requestBytes.Length, SocketFlags.None);
            int bytesReceived = socket.Receive(responseBytes, 2, SocketFlags.None);
            
            if (bytesReceived == 2 && responseBytes[1] == (byte)Authentication.None)
            {
                OnStatusChanged(socket, "Successfully negotiated with the proxy!");
                return socket;
            }

            OnStatusChanged(socket, "Failed to negotiate with the proxy!");
            return null;
        }

        private static Socket Handshake(Socket socket, IPAddress targetHost, int targetPort)
        {
            int i = 0;
            byte[] addressBytes = targetHost.GetAddressBytes();
            byte[] portBytes = new byte[2] { (byte)(targetPort / 256), (byte)(targetPort % 256) };
            byte[] requestBytes = new byte[4 + addressBytes.Length + portBytes.Length];
            byte[] responseBytes = new byte[2];
            requestBytes[i++] = (byte)SocksType.Socks5;
            requestBytes[i++] = 0x01;
            requestBytes[i++] = 0x00;
            requestBytes[i++] = 0x01;
            
            addressBytes.CopyTo(requestBytes, i);
            i += addressBytes.Length;

            for (int b = 0; b < portBytes.Length; b++)
                requestBytes[i++] = portBytes[b];

            socket.Send(requestBytes, i, SocketFlags.None);
            int receivedBytes = socket.Receive(responseBytes, SocketFlags.None);

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