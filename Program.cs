using System;
using System.Threading;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace SocksTcpClient
{
    class Program
    {
        static void Main(string[] args)
        {
            SocksTcpClient.StatusChanged += SocksTcpClient_OnStatusChanged;
            TcpClient client = SocksTcpClient.Connect("127.0.0.1", 9150, "94.215.86.127", 65432);

            NetworkStream stream = client.GetStream();

            Thread trd = new Thread(() =>
            {
                while (true)
                {
                    if (stream.DataAvailable && stream.CanRead)
                    {
                        byte[] responseBytes = new byte[8192];
                        int responseLength = stream.Read(responseBytes, 0, responseBytes.Length);
                        string response = Encoding.ASCII.GetString(responseBytes, 0, responseLength - 1);
                        Console.WriteLine(response);
                    }
                }
            });
            trd.Start();

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("GET http://www.seafight.nl/ HTTP/1.1");
            builder.AppendLine("");

            byte[] requestBytes = Encoding.ASCII.GetBytes(builder.ToString());
            stream.Write(requestBytes, 0, requestBytes.Length);
            stream.Flush();

            Console.ReadLine();
        }

        private static void SocksTcpClient_OnStatusChanged(object sender, string status)
        {
            Console.WriteLine(status);
        }
    }
}