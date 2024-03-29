using System;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace IPK24Chat
{
    class TcpChatClient
    {
        private TcpClient client;
        // private int serverPort;
        private NetworkStream stream;
        public bool autorized = false;
        private string? displayName; 
        
        private string baseRegex = @"^[A-Za-z0-9-]+$";

        // public TcpChatClient(TcpClient client, NetworkStream stream)
        // {
        //     this.client = client;
        //     this.stream = stream;
        // }
        
        
        private async Task ListenForServer(CancellationToken token)
        {
            // NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];

            while (!token.IsCancellationRequested)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false);
                
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
                // if (!client.Connected)
                // {
                //     Console.Error.WriteLine("ERR: Unable to connect to server.");
                //     Environment.Exit(1);
                // }
                // else
                // {
                //     Console.WriteLine($"Connected to server at {serverAddress}:{serverPort}");
                // }
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
            if (client == null  || stream == null)
            {
                if (client == null)
                {
                    Console.Error.WriteLine("ERR: Client is null");
                }

                // if (!client.Connected)
                // {
                //     Console.Error.WriteLine("ERR: Client is not connected");
                // }

                if (stream == null)
                {
                    Console.Error.WriteLine("ERR: Stream is null");
                }
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
            stream.Close();
            client.Close();
        }

        public void StartInteractiveSession()
        {
            // Console.CancelKeyPress += (sender, e) => {
            //     e.Cancel = true; // Prevents the program from terminating.
            //     Disconnect();
            // };

            // if (!client.Connected)
            // {
            //     Console.Error.WriteLine("ERR: Unable to connect to server.");
            //     Environment.Exit(1);
            // }
            // else
            // {
            //     Console.WriteLine($"Connected to server at");
            // }

            while (true) 
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                Task listenTask = ListenForServer(cts.Token);
                Console.CancelKeyPress += (sender, e) => {
                    e.Cancel = true; // Prevents the program from terminating.
                    cts.Cancel();
                    Thread.Sleep(300);
                    Disconnect();
                    Environment.Exit(0);
                };
                string input = Console.ReadLine();

                if (string.IsNullOrEmpty(input))
                {
                    Disconnect();
                    Environment.Exit(0);
                }

                // if (input == "EOF"){
                //     Disconnect();
                //     Environment.Exit(0);
                // }

                // if (!client.Connected)
                // {
                //     Console.Error.WriteLine("ERR: Unable to connect to server.");
                //     Environment.Exit(1);
                // }
                // else
                // {
                //     Console.WriteLine($"Connected to server at");
                // }

                if (input.StartsWith("/"))
                {
                    ProcessCommand(input, cts);
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
                    // else if (input.Length > 1400 || !Regex.IsMatch(input, baseRegex))
                    else if (input.Length > 1400)
                    {
                        Console.WriteLine(input);
                        Console.Error.WriteLine("ERR: Message too long. Max length is 1400 characters. Message must be alphanumeric");
                        continue;
                    }

                    SendMessage($"MSG FROM {displayName} IS {input}");
                    // string reply = ReceiveMessage();
                    // ProcessServerReply(reply);
                }
            }
        }

        private void ProcessCommand(string command, CancellationTokenSource cts)
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
                    // if (!client.Connected)
                    // {
                    //     Console.Error.WriteLine("ERR: Unable to connect to server before cts.");
                    //     Environment.Exit(1);
                    // }
                    // else
                    // {
                    //     Console.WriteLine($"Connected to server at before cts");
                    // }    
                    cts.Cancel(); 
                    // if (!client.Connected)
                    // {
                    //     Console.Error.WriteLine("ERR: Unable to connect to server after cts.");
                    //     Environment.Exit(1);
                    // }
                    // else
                    // {
                    //     Console.WriteLine($"Connected to server at after cts");
                    // }               
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
                    cts.Cancel();
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
            if(username.Length > 20 || secret.Length > 128 || newName.Length > 20 || 
            !Regex.IsMatch(username, baseRegex) || !Regex.IsMatch(secret, baseRegex) || !Regex.IsMatch(newName, baseRegex))
            {
                Console.Error.WriteLine("ERR: Invalid input. Username, secret and display name must be alphanumeric and have a maximum length of 20, 128 and 20 characters respectively.");
                return;
            }
            // if (!client.Connected)
            // {
            //     Console.Error.WriteLine("ERR: Unable to connect to server send.");
            //     Environment.Exit(1);
            // }
            // else
            // {
            //     Console.WriteLine($"Connected to server at send");
            // }    
            SendMessage($"AUTH {username} AS {newName} USING {secret}");
            displayName = newName; 
            Thread.Sleep(300);
            byte[] buffer = new byte[1024];          
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
            string trimMessage = message.TrimEnd('\r', '\n');
            ProcessServerReply(trimMessage);   
            // Console.WriteLine("Auth done");   
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
            Thread.Sleep(300);
            byte[] buffer = new byte[1024];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
            ProcessServerReply(message);
        }

        private void ProcessServerReply(string reply)
        {
            switch (reply)
            {
                case string r when r.StartsWith("REPLY OK IS"):
                    // Console.WriteLine("Reply OK IS:");
                    Console.Error.WriteLine("Success:" + r.Substring("REPLY OK IS".Length));
                    autorized = true;
                    break;

                case string r when r.StartsWith("REPLY NOK IS"):
                    // Console.WriteLine("Reply NOK IS:");
                    Console.Error.WriteLine("Failure:" + r.Substring("REPLY NOK IS".Length));
                    // Console.WriteLine("Reply NOK IS:");
                    break;

                case string r when r.StartsWith("ERR FROM"):
                    string errDisplayName = r.Substring("ERR FROM".Length, r.IndexOf("IS") - "ERR FROM".Length - 1);
                    string errMessage = r.Substring(r.IndexOf("IS") + 3);
                    Console.Error.WriteLine($"ERR FROM{errDisplayName}: {errMessage}");
                    Disconnect();
                    break;

                case string r when r.StartsWith("MSG FROM") && r.Contains("IS"):
                    // Console.WriteLine("MSG FROM:");
                    int fromIndex = r.IndexOf("FROM") + 5;
                    int isIndex = r.IndexOf("IS", fromIndex);
                    string messageDisplayName = r.Substring(fromIndex, isIndex - fromIndex - 1);
                    string messageContent = r.Substring(isIndex + 3);
                    if (messageContent.Length > 1400)
                    {
                        Console.Error.WriteLine("ERR: Message too long. Max length is 1400 characters.");
                        return;
                    }
                    Console.WriteLine($"{messageDisplayName}: {messageContent}");
                    break;

                case string r when r.Contains("BYE"):
                    
                    Disconnect();
                    break;

                default:
                    Console.Error.WriteLine("ERR: Unknown server reply.");
                    SendMessage($"ERR FROM {displayName} IS {reply}");
                    break;
            }

            // Console.WriteLine("End of ProcessServerReply");

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
