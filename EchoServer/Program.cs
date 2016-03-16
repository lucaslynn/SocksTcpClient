using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace EchoServer
{
    class Program
    {
        static void Main(string[] args)
        {
            TcpListener listener = new TcpListener(IPAddress.Parse("192.168.178.19"), 50123);
            listener.Start();
            TcpClient client = listener.AcceptTcpClient();
            NetworkStream clientStream = client.GetStream();
            StreamReader clientReader = new StreamReader(clientStream);
            StreamWriter clientWriter = new StreamWriter(clientStream);
            string clientInput = clientReader.ReadLine();
            clientWriter.WriteLine(clientInput);
            clientWriter.Flush();
            Console.WriteLine("Server done echoing");
            Console.ReadKey();
        }
    }
}