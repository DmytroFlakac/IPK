using System;
using System.Net.Sockets;
using System.Text;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;

class Program
{
    static void Main(string[] args)
    {
        string serverAddress = "192.168.1.73"; // Replace with the server IP or hostname
        int serverPort = 4567; // Replace with the server port

        TcpChatClient client = new TcpChatClient();
        client.Connect(serverAddress, serverPort);

        Console.WriteLine("Connected to server. Type '/help' for a list of commands.");

        while (true)
        {
            string input = Console.ReadLine();

            if (input == "/auth")
            {
                Console.Write("Enter username: ");
                string username = Console.ReadLine();
                Console.Write("Enter display name: ");
                string displayName = Console.ReadLine();
                Console.Write("Enter secret: ");
                string secret = Console.ReadLine();

                string authMessage = $"AUTH {username} AS {displayName} USING {secret}\r\n";
                client.SendMessage(authMessage);

                string reply = client.ReceiveMessage();
                Console.WriteLine(reply);
            }
            else if (input == "/join")
            {
                Console.Write("Enter channel ID: ");
                string channelId = Console.ReadLine();
                Console.Write("Enter display name: ");
                string displayName = Console.ReadLine();

                string joinMessage = $"JOIN {channelId} AS {displayName}\r\n";
                client.SendMessage(joinMessage);

                string reply = client.ReceiveMessage();
                Console.WriteLine(reply);
            }
            else if (input == "/msg")
            {
                Console.Write("Enter display name: ");
                string displayName = Console.ReadLine();
                Console.Write("Enter message: ");
                string messageContent = Console.ReadLine();

                string message = $"MSG FROM {displayName} IS {messageContent}\r\n";
                client.SendMessage(message);
            }
            else if (input == "/bye")
            {
                client.SendMessage("BYE\r\n");
                client.Disconnect();
                break;
            }
            else if (input == "/help")
            {
                Console.WriteLine("Available commands:");
                Console.WriteLine("/auth - Authenticate with the server");
                Console.WriteLine("/join - Join a channel");
                Console.WriteLine("/msg - Send a message to the current channel");
                Console.WriteLine("/bye - Disconnect from the server");
                Console.WriteLine("/help - Show this help message");
            }
            else
            {
                Console.WriteLine("Invalid command. Type '/help' for a list of available commands.");
            }
        }
    }
    

    class TcpChatClient
    {
        private TcpClient client;
        private NetworkStream stream;

        public void Connect(string serverAddress, int serverPort)
        {
            client = new TcpClient();
            client.Connect(serverAddress, serverPort);
            stream = client.GetStream();
        }

        public void SendMessage(string message)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(message + "\r\n");
            stream.Write(buffer, 0, buffer.Length);
        }

        public string ReceiveMessage()
        {
            byte[] buffer = new byte[1024];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            return Encoding.ASCII.GetString(buffer, 0, bytesRead);
        }

        public void Disconnect()
        {
            stream.Close();
            client.Close();
        }
    }
}

