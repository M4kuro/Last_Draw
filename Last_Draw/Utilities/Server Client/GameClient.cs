using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Last_Draw.Utilities.Server_Client
{
    public class GameClient
    {

        private TcpClient _client;
        private NetworkStream _stream;


        public void ConnectToServer()
        {
            try
            {
                // Connect to the server
                _client = new TcpClient("127.0.0.1", 12345);
                _stream = _client.GetStream();

                Console.WriteLine("Connected to server.");
                SendMessage("Hello from the client!");

                // Handle incoming messages
                ReceiveMessages();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error connecting to server: {ex.Message}");
            }
        }

        private void SendMessage(string message)
        {
            if (_stream == null) return;

            byte[] data = Encoding.UTF8.GetBytes(message);
            _stream.Write(data, 0, data.Length);
        }

        private void ReceiveMessages()
        {
            byte[] buffer = new byte[1024];

            try
            {
                while (true)
                {
                    int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break; // Connection closed

                    string receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"Server says: {receivedMessage}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving messages: {ex.Message}");
            }
            finally
            {
                _client.Close();
            }
        }
    }
}
