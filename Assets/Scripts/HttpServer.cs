using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class HttpServer
{
	static Encoding enc = Encoding.UTF8;

	public static async Task Start(int port)
	{
		Console.WriteLine("Starting...");
		var server = TcpListener.Create(port);
		server.Start();
		Console.WriteLine("Started.");
		while (true)
		{
			using (var tcpClient = await server.AcceptTcpClientAsync())
			{
				try
				{
					Console.WriteLine("[Server] Client has connected");
					using (var networkStream = tcpClient.GetStream())
					using (var reader = new StreamReader(networkStream))
					using (var writer = new StreamWriter(networkStream) { AutoFlush = true })
					{
						var buffer = new byte[4096];
						Console.WriteLine("[Server] Reading from client");
						var request = await reader.ReadLineAsync();
						string.Format(string.Format("[Server] Client wrote '{0}'", request));

						await writer.WriteLineAsync($"Helllooooo world!");
					}
				}
				catch (Exception)
				{
					Console.WriteLine("[Server] client connection lost");
				}
			}
		}
	}

	public static string ToString(NetworkStream stream)
	{
		MemoryStream memoryStream = new MemoryStream();
		byte[] data = new byte[256];
		int size;
		do
		{
			size = stream.Read(data, 0, data.Length);
			if (size == 0)
			{
				Console.WriteLine("client disconnected...");
				Console.ReadLine();
				return null;
			}
			memoryStream.Write(data, 0, size);
		} while (stream.DataAvailable);
		return enc.GetString(memoryStream.ToArray());
	}
}