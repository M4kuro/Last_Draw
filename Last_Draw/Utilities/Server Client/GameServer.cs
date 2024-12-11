using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Last_Draw.Utilities.Server_Client
{



    public class GameServer
    {

        private TcpListener _listener;
        private bool _isRunning;

        public void StartServer()
        {
            try
            {
                // Start listening on a port
                _listener = new TcpListener(IPAddress.Any, 12345);
                _listener.Start();
                _isRunning = true;

                Console.WriteLine("Server is running on port 12345...");
                Thread listenThread = new Thread(HandleConnections);
                listenThread.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting server: {ex.Message}");
            }

        }

        private void HandleConnections()
        {
            while (_isRunning)
            {
                try
                {
                    TcpClient client = _listener.AcceptTcpClient();
                    Console.WriteLine("Client connected!");

                    // Handle client communication
                    Thread clientThread = new Thread(() => HandleClient(client));
                    clientThread.Start();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error handling connection: {ex.Message}");
                }
            }
        }

        private void HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];

            try
            {
                while (true)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break; // Connection closed

                    string receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"Received: {receivedMessage}");

                    // Echo the message back to the client
                    byte[] response = Encoding.UTF8.GetBytes("Message received");
                    stream.Write(response, 0, response.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling client: {ex.Message}");
            }
            finally
            {
                client.Close();
            }
        }

        public void StopServer()
        {
            _isRunning = false;
            _listener?.Stop();
        }


    }
}
