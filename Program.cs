using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocksTcpClient
{
    class Program
    {
        static void Main(string[] args)
        {
            SocksTcpClient client = new SocksTcpClient();
            client.StatusChanged += SocksTcpClient_OnStatusChanged;
            client.Connect("206.192.215.78", 10000, "88.159.9.139", 1234);

            Console.ReadLine();
        }

        private static void SocksTcpClient_OnStatusChanged(object sender, string status)
        {
            Console.WriteLine(status);
        }
    }
}