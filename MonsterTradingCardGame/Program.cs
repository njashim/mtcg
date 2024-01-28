using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MonsterTradingCardGame
{
    public class Program
    {
        public static async Task Main()
        {
            // IP address and port to listen on
            IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
            int port = 10001;

            TcpListener tcpListener = new TcpListener(ipAddress, port);

            // listening for incoming connections
            tcpListener.Start();
            Console.WriteLine($"Server started. Listening on {ipAddress}:{port}");

            Database db = new Database();

            while (true)
            {
                // Wait for a client connection
                TcpClient client = await tcpListener.AcceptTcpClientAsync();

                // Handle the client asynchronously
                _ = Task.Run(() => HandleClientAsync(client, tcpListener, db));
            }
        }

        public static async Task HandleClientAsync(TcpClient client, TcpListener listener, Database db)
        {
            Console.WriteLine($"Client connected: {client.Client.RemoteEndPoint}");

            // client's network stream
            using (NetworkStream networkStream = client.GetStream())
            {
                try
                {
                    var requestBytes = new byte[1024];
                    await networkStream.ReadAsync(requestBytes, 0, requestBytes.Length);
                    string request = Encoding.UTF8.GetString(requestBytes);
                    Console.WriteLine($"Received request from {client.Client.RemoteEndPoint}:\r\n{request}\r\n");

                    RequestHandler requestHandler = new RequestHandler(client, listener, request, db);

                    Console.WriteLine($"Sending response:\r\n{requestHandler.Response}");

                    byte[] responseData = Encoding.UTF8.GetBytes(requestHandler.Response);
                    await networkStream.WriteAsync(responseData, 0, responseData.Length);
                    await networkStream.FlushAsync();
                }
                catch (IOException e)
                {
                    Console.WriteLine(e.Message);
                }
                finally
                {
                    Console.WriteLine($"Client disconnected: {client.Client.RemoteEndPoint}");
                    client.Close();
                }
            }
        }
    }
}