﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace UDPBroadcastTest
{
    class Program
    {
        static UdpClient udpClient;
        static int PORT = 9876;

        static async Task Main(string[] args)
        {
            udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, PORT));

            ReceiveAndWrite();

            var input = string.Empty;
            while (true)
            {
                Console.Write("Send->");
                input = Console.ReadLine();
                var data = Encoding.UTF8.GetBytes(input);
                await udpClient.SendAsync(data, data.Length, IPAddress.Broadcast.ToString(), PORT);
            }
        }

        static Task ReceiveAndWrite()
        {
            return Task.Run(async () =>
            {
                while (true)
                {
                    var recvBuffer = await udpClient.ReceiveAsync();
                    Console.WriteLine($"Received: {Encoding.UTF8.GetString(recvBuffer.Buffer)}");
                }
            });

        }
    }
}
