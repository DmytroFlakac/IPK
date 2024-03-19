using System;
using System.Net.Sockets;
using System.Text;

namespace IPK24Chat
{
    class TcpChatClient
    {
        private TcpClient? client;
        private NetworkStream? stream;
        public bool autorized = false;
        private string? displayName; // Holds the current user's display name
        
        
        private async Task ListenForServer(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];

            while (true)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    Console.WriteLine($"Client disconnected: {client.Client.RemoteEndPoint}");
                    break;
                }

                string message = Encoding.ASCII.GetString(buffer, 0, bytesRead).TrimEnd('\r', '\n');
                ProcessServerReply(message);
                
            }
        }
    

        public void Connect(string serverAddress, int serverPort)
        {
            try
            {
                client = new TcpClient();
                client.Connect(serverAddress, serverPort);
                stream = client.GetStream();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"ERR: Unable to connect to server - {e.Message}");
                Environment.Exit(1);
            }
        }

        public void SendMessage(string message)
        {
            if (client == null || !client.Connected || stream == null)
            {
                Console.Error.WriteLine("ERR: Not connected to any server. Please connect first.");
                return;
            }

            byte[] buffer = Encoding.ASCII.GetBytes(message + "\r\n");
            stream.Write(buffer, 0, buffer.Length);
        }

        public string ReceiveMessage()
        {
            byte[] buffer = new byte[1024];
            int bytesRead = stream?.Read(buffer, 0, buffer.Length) ?? 0;
            return Encoding.ASCII.GetString(buffer, 0, bytesRead).TrimEnd('\r', '\n');
        }

        public void Disconnect()
        {
            SendMessage("BYE");
            stream?.Close();
            client?.Close();
        }

        public void StartInteractiveSession()
        {
            var listenTask = ListenForServer(client);
            Console.CancelKeyPress += (sender, e) => {
                e.Cancel = true; // Prevents the program from terminating.
                Disconnect();
            };

            while (true)
            {
                string input = Console.ReadLine();

                if (string.IsNullOrEmpty(input))
                {
                    continue;
                }

                if (input.StartsWith("/"))
                {
                    ProcessCommand(input, listenTask);
                }
                else
                {
                    if (displayName == null)
                    {
                        Console.Error.WriteLine("ERR: Display name not set. Use /auth or /rename to set a display name.");
                        continue;
                    }
                    else if (!autorized)
                    {
                        Console.Error.WriteLine("ERR: Not authorized. Use /auth to authenticate.");
                        continue;
                    }

                    SendMessage($"MSG FROM {displayName} IS {input}");
                    // string reply = ReceiveMessage();
                    // ProcessServerReply(reply);
                }
            }
        }

        private void ProcessCommand(string command, Task listenTask)
        {
            string[] parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string commandType = parts[0].ToLower();

            switch (commandType)
            {
                case "/auth":
                    if (parts.Length != 4)
                    {
                        Console.WriteLine(parts.Length);
                        Console.Error.WriteLine("ERR: Incorrect /auth usage. Expected /auth {Username} {Secret} {DisplayName}");
                        return;
                    }
                    else if (autorized)
                    {
                        Console.Error.WriteLine("ERR: Already authorized. Use /rename to change display name.");
                        return;
                    }
                    HandleAuth(parts[1], parts[2], parts[3]);
                    break;
                case "/join":
                    if (parts.Length != 2)
                    {
                        Console.Error.WriteLine("ERR: Incorrect /join usage. Expected /join {ChannelID}");
                        return;
                    }
                    else if (!autorized)
                    {
                        Console.Error.WriteLine("ERR: Not authorized. Use /auth to authenticate.");
                    }
                    // listenTask.Wait();
                    HandleJoin(parts[1]);
                    break;
                case "/rename":
                    if (parts.Length != 2)
                    {
                        Console.Error.WriteLine("ERR: Incorrect /rename usage. Expected /rename {DisplayName}");
                        return;
                    }
                    displayName = parts[1];
                    Console.WriteLine($"Display name changed to: {displayName}");
                    break;
                case "/bye":
                    Disconnect();
                    Environment.Exit(0);
                    break;
                case "/help":
                    ShowHelp();
                    break;
                default:
                    Console.Error.WriteLine("ERR: Unknown command. Type '/help' for a list of available commands.");
                    break;
            }
        }

        private void HandleAuth(string username, string secret, string newName)
        {
            SendMessage($"AUTH {username} AS {newName} USING {secret}");
            displayName = newName; // Set local display name
            string reply = ReceiveMessage();
            ProcessServerReply(reply);
            autorized = true;
        }

        private void HandleJoin(string channelId)
        {
            SendMessage($"JOIN {channelId} AS {displayName}");
            string reply = ReceiveMessage();
            ProcessServerReply(reply);
        }

        private void ProcessServerReply(string reply)
        {
            if (reply.StartsWith("REPLY OK IS"))
            {
                Console.WriteLine("Success: " + reply.Substring("REPLY OK IS".Length));
            }
            else if (reply.StartsWith("REPLY NOK IS"))
            {
                Console.Error.WriteLine("Failure: " + reply.Substring("REPLY NOK IS".Length));
            }
            else if (reply.StartsWith("ERR FROM"))
            {
                Console.Error.WriteLine(reply);
            }
            else if (reply.StartsWith("MSG FROM"))
            {
                // Extract DisplayName and MessageContent for incoming MSG
                int fromIndex = reply.IndexOf("FROM") + 5; // Start after "FROM "
                int isIndex = reply.IndexOf("IS", fromIndex);
                string messageDisplayName = reply.Substring(fromIndex, isIndex - fromIndex - 1);
                string messageContent = reply.Substring(isIndex + 3); // Start after "IS "
                Console.WriteLine($"{messageDisplayName}: {messageContent}");
            }
        }

        private void ShowHelp()
        {
            Console.WriteLine("Supported commands:");
            Console.WriteLine("/auth {Username} {Secret} {DisplayName} - Authenticate and set display name");
            Console.WriteLine("/join {ChannelID} - Join a channel with the specified ID");
            Console.WriteLine("/rename {DisplayName} - Change your display name");
            Console.WriteLine("/help - Show this help message");
        }

        // Additional methods for processing specific commands and handling server replies...
    }
}
