using System;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace IPK24Chat
{
    class TcpChatClient
    {
        private TcpClient? client;
        // private int serverPort;
        private NetworkStream? stream;
        public bool autorized = false;
        private string? displayName; 
        
        private string baseRegex = @"^[A-Za-z0-9-]+$";

        // public TcpChatClient(TcpClient client, int serverPort)
        // {
        //     this.client = client;
        //     this.serverPort = serverPort;
        // }
        
        
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
                string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                // ProcessServerReply(message);  
                foreach (string line in message.Split("\r\n", StringSplitOptions.RemoveEmptyEntries))
                {
                    ProcessServerReply(line);
                }           
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
            Task listenTask = ListenForServer(client!);
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
                    else if (input.Length > 1400 || !Regex.IsMatch(input, baseRegex))
                    {
                        Console.Error.WriteLine("ERR: Message too long. Max length is 1400 characters. Message must be alphanumeric");
                        continue;
                    }

                    SendMessage($"MSG FROM {displayName} IS {input}");
                    // string reply = ReceiveMessage();
                    // ProcessServerReply(reply);
                }
            }
        }

        private async void ProcessCommand(string command, Task listenTask)
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
                    // listenTask.Wait();                   
                    HandleAuth(parts[1], parts[2], parts[3]);
                    await listenTask;
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
                    HandleJoin(parts[1]);
                    await listenTask;
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
            if(username.Length > 20 || secret.Length > 128 || newName.Length > 20 || 
            !Regex.IsMatch(username, baseRegex) || !Regex.IsMatch(secret, baseRegex) || !Regex.IsMatch(newName, baseRegex))
            {
                Console.Error.WriteLine("ERR: Invalid input. Username, secret and display name must be alphanumeric and have a maximum length of 20, 128 and 20 characters respectively.");
                return;
            }
            SendMessage($"AUTH {username} AS {newName} USING {secret}");
            displayName = newName; 
            autorized = true;
        }

        private void HandleJoin(string channelId)
        {
            // if (channelId.Length > 20 || !Regex.IsMatch(channelId, baseRegex))
            // {
            //     Console.Error.WriteLine("ERR: Invalid input. Channel ID must be alphanumeric and have a maximum length of 20 characters.");
            //     return;
            // }
            if (channelId.Length > 20)
            {
                Console.Error.WriteLine("ERR: Invalid input. Channel ID must be alphanumeric and have a maximum length of 20 characters.");
                return;
            }
            SendMessage($"JOIN {channelId} AS {displayName}");
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
            else if (reply.StartsWith("MSG FROM") && reply.Contains("IS"))
            { 
                int fromIndex = reply.IndexOf("FROM") + 5; 
                int isIndex = reply.IndexOf("IS", fromIndex);
                string messageDisplayName = reply.Substring(fromIndex, isIndex - fromIndex - 1);
                string messageContent = reply.Substring(isIndex + 3); 
                if(messageContent.Length > 1400)
                {
                    Console.Error.WriteLine("ERR: Message too long. Max length is 1400 characters.");
                    return;
                }
                Console.WriteLine($"{messageDisplayName}: {messageContent}");
            }
            else if(reply.Contains("BYE"))
            {
                Console.WriteLine("Server disconnected.");
                Disconnect();
                Environment.Exit(0);
            }
            else
            {
                Console.Error.WriteLine("ERR: Unknown server reply.");
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
    }
}
