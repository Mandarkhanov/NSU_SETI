﻿using System.Net;
using System.Net.Sockets;
using Lab2.Client;
using Lab2.Server;

public class Program
{
    public static async Task Main(string[] args)
    {
        if (args[0] == "server")
        {
            Server server = new Server(Int32.Parse(args[1]));
            server.StartServer();
        }
        else if (args[0] == "client")
        {
            Client client;
            IPAddress address;

            try
            {
                client = IPAddress.TryParse(args[2], out address)
                    ? new Client(args[1], address, Int32.Parse(args[3]))
                    : new Client(args[1], new DnsEndPoint(args[2], Int32.Parse(args[3])));
            }
            catch (SocketException e)
            {
                Console.WriteLine(e);
                return;
            }

            await client.HandleFile();
        }
        else
        {
            Console.WriteLine("Invalid first argument. Use \"server\" or \"client\".");
        }
    }
}