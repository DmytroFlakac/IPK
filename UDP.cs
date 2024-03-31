using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace IPK24Chat
{
    class UdpChatClient{
        private UdpClient client;
        private string serverAddress;
        private int serverPort;
        private int udpRetryCount;
        private string? displayName;
        private bool autorized = false;
        private int messageID = -1;
        private static string baseRegex = @"^[a-zA-Z0-9\s]*$";
        

        public UdpChatClient(string serverAddress, int serverPort, int udpConfirmationTimeout, int udpRetryCount, UdpClient client)
        {
            this.client = client;
            this.serverAddress = serverAddress;
            this.serverPort = serverPort;
            client.Client.ReceiveTimeout = udpConfirmationTimeout;
            client.Client.SendTimeout = udpConfirmationTimeout;
            this.udpRetryCount = udpRetryCount;
        }

        // public async Task StartAsync()
        // {
        //     await Task.Run(() => StartInteractiveSession());
        // }

        public async Task ListenForServer(UdpClient client, CancellationToken cts)
        {
            IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.Any, 0);
            try
            {
                while (!cts.IsCancellationRequested)
                {                    
                    UdpReceiveResult result = await client.ReceiveAsync(cts).ConfigureAwait(false);
                    byte[] message = result.Buffer;
                    serverEndpoint = result.RemoteEndPoint;
                    int messageID = UDPmessageHelper.getMessageID(message);
                    MessageType messageType = UDPmessageHelper.getMessageType(message);
                    // Console.WriteLine("listenForServer");
                    if(messageType == MessageType.MSG)
                    {
                        sendConfirmation(messageID);
                        string msg = UDPmessageHelper.getMSGContents(message);
                        string displayname = UDPmessageHelper.getMSGdisplayName(message);
                        Console.WriteLine($"{displayname}: {msg}");
                    }
                    else if(messageType == MessageType.ERR)
                    {
                        sendConfirmation(messageID);
                        string msg = UDPmessageHelper.getMSGContents(message);
                        string displayname = UDPmessageHelper.getMSGdisplayName(message);
                        Console.Error.WriteLine($"ERR FROM {displayname}: {msg}");
                        if(cts.IsCancellationRequested)
                            cts.ThrowIfCancellationRequested();
                        Disconnect();
                    } 
                    else if(messageType == MessageType.BYE)
                    {
                        sendConfirmation(messageID);
                        // Console.WriteLine("Server disconnected listen.");
                        if(cts.IsCancellationRequested)
                            cts.ThrowIfCancellationRequested();
                        Disconnect();
                    }  
                    else
                    {
                        sendConfirmation(messageID);
                        Console.Error.WriteLine("ERR: Unexpected message type.");
                        byte[] errorMessage = UDPmessageHelper.buildErrorMessage(messageID, "Unexpected message type.", displayName, MessageType.ERR);
                        client.Send(errorMessage, errorMessage.Length, serverEndpoint);
                        waitConfirmation(errorMessage);
                    }     
                }
            }
            catch (SocketException e)
            {
                Console.Error.WriteLine($"SocketException: {e.Message}");
            }
        }

        public void StartInteractiveSession()
        {
            while (true)
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                Task listenTask = ListenForServer(client, cts.Token);
                Console.CancelKeyPress += (sender, e) => {
                    
                    e.Cancel = true; 
                    cts.Cancel();
                    Thread.Sleep(300);
                    // Console.WriteLine("Server disconnected main.");
                    Disconnect();
                };
                Thread.Sleep(300);
                string input = Console.ReadLine();

                if (string.IsNullOrEmpty(input))
                {
                    cts.Cancel();
                    Thread.Sleep(300);
                    Disconnect();

                }

                if (input.StartsWith("/"))
                {
                    ProcessCommand(input, cts, listenTask);
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
                    cts.Cancel();
                    HandleMessage(input);
                }
            }
        }

        private void ProcessCommand(string command, CancellationTokenSource cts, Task listenTask)
        {
            string[] parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string commandType = parts[0].ToLower();

            switch (commandType)
            {
                case "/auth":
                    if (parts.Length != 4)
                    {
                        // Console.WriteLine(parts.Length);
                        Console.Error.WriteLine("ERR: Incorrect /auth usage. Expected /auth {Username} {Secret} {DisplayName}");
                        return;
                    }
                    else if (autorized)
                    {
                        Console.Error.WriteLine("ERR: Already authorized. Use /rename to change display name.");
                        return;
                    }  
                    cts.Cancel();                      
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
                // Console.WriteLine("Server disconnected user.");
                    cts.Cancel();
                    Thread.Sleep(300);
                    Disconnect();
                    break;
                case "/help":
                    ShowHelp();
                    break;
                default:
                    Console.Error.WriteLine("ERR: Unknown command. Type '/help' for a list of available commands.");
                    break;
            }
        }


        private void Disconnect()
        {
            ++messageID;
            byte[] messageBytes = UDPmessageHelper.buildBYEMessage(messageID, MessageType.BYE);
            // string hex = BitConverter.ToString(messageBytes);
            // Console.WriteLine(hex);
            client.Send(messageBytes, messageBytes.Length, serverAddress, serverPort);
            waitConfirmation(messageBytes);
            client.Close();
            Environment.Exit(0);
        }

        private void HandleAuth(string username, string secret, string displayName)
        {
            ++messageID;
            byte[] messageBytes = UDPmessageHelper.buildMessage(username, secret, displayName, messageID, MessageType.AUTH);
            client.Send(messageBytes, messageBytes.Length, serverAddress, serverPort);
            this.displayName = displayName;
            // Console.WriteLine("Waiting for server reply in HandleAuth...");
            if(!waitConfirmation(messageBytes))            
                return;   
            // Console.WriteLine("Waiting for server reply...");         
            if(waitReply())
                autorized = true;
            // Console.WriteLine("Waiting for server reply...");
        }

        private void HandleJoin(string channelID)
        {
            ++messageID;
            byte[] messageBytes = UDPmessageHelper.buildMessage(channelID, messageID, displayName, MessageType.JOIN);
            client.Send(messageBytes, messageBytes.Length, serverAddress, serverPort);
            // Console.WriteLine("Waiting for server reply in HandleJoin...");
            if(!waitConfirmation(messageBytes))            
                return;
            waitReply();
        }

        private void HandleMessage(string message)
        {
            ++messageID;
            byte[] messageBytes = UDPmessageHelper.buildMessage(messageID, message, displayName, MessageType.MSG);
            client.Send(messageBytes, messageBytes.Length, serverAddress, serverPort);
            // Console.WriteLine("Waiting for server reply in HandleMessage...");
            if(!waitConfirmation(messageBytes))            
                return;
            //waitReply();
        }

        // public async Task ListenForServer(CancellationTokenSource cts)
        // {
        //     //pass
        // }

        private void ProcessServerReply(string reply)
        {
            //pass
        }

        // private void SendMessage(byte[] messageBytes)
        // {
        //     client.Send(messageBytes, messageBytes.Length, serverAddress, serverPort);
        // }

        public void sendConfirmation(int messageID)
        {
            // UDPmessageHelper.printMessage(UDPmessageHelper.buildMessage(messageID, MessageType.CONFIRM));
            // Console.WriteLine("Sending confirmation.");
            byte[] messageBytes = UDPmessageHelper.buildMessage(messageID, MessageType.CONFIRM);
            client.Send(messageBytes, messageBytes.Length, serverAddress, serverPort);
        }

        private bool waitConfirmation(byte[] messageBytes)
        {
            
            for (int i = 0; i < udpRetryCount; i++)
            {
                try
                {
                    // Console.WriteLine($"{udpRetryCount - i} retries left.");
                    IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.Any, 0);
                    // Thread.Sleep(300);
                    byte[] response = client.Receive(ref serverEndpoint);
                    // UDPmessageHelper.printMessage(response);
                    // string hex = BitConverter.ToString(response);
                    // Console.WriteLine(hex);
                    // Console.WriteLine("Waiting for confirmation.");
                    if (UDPmessageHelper.getMessageType(response) == MessageType.CONFIRM && 
                    UDPmessageHelper.getMessageID(response) == messageID)
                    {
                        // Console.WriteLine("Success: Server confirmed message.");
                        return true;
                    }
                    else
                    {
                        client.Send(messageBytes, messageBytes.Length, serverAddress, serverPort);
                    }
                }
                catch (SocketException e)
                {
                    if (e.SocketErrorCode == SocketError.TimedOut)
                    {
                        Console.WriteLine("Timeout: Server did not reply.");
                        continue;
                    }
                }
            }

            // Console.WriteLine("Failure: Server did not confirm message.");
            return false;
        }

        private bool waitReply()
        {
            // Console.WriteLine("Waiting for reply started.");
            for (int i = 0; i <= udpRetryCount; i++)
            {
                // Console.WriteLine("Waiting for reply in loop.");
                try
                {
                    // Console.WriteLine("_____________________________________________________________");
                    IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.Any, 0); 
                    // Thread.Sleep(300);                  
                    // Console.WriteLine(serverEndpoint.Address);
                    // Console.WriteLine(serverEndpoint.Port);
                    // Console.WriteLine("_____________________________________________________________");
                    byte[] response = client.Receive(ref serverEndpoint);
                    // Console.WriteLine("Reply received.");
                    // UDPmessageHelper.printMessage(response);
                    serverPort = serverEndpoint.Port;
                    int responseID = UDPmessageHelper.getMessageID(response);
                    // UDPmessageHelper.printMessage(response);
                    // string hex = BitConverter.ToString(response);
                    // Console.WriteLine(hex);
                    // Console.WriteLine("REPLY");
                    sendConfirmation(responseID);
                    // if(UDPmessageHelper.getMessageType(response) == MessageType.CONFIRM)
                    // {
                    //     continue;
                    // }
                    if(UDPmessageHelper.getMessageType(response) != MessageType.REPLY)
                    {
                        string message = UDPmessageHelper.getMSGContents(response);
                        string displayname = UDPmessageHelper.getMSGdisplayName(response);
                        if(UDPmessageHelper.getMessageType(response) == MessageType.MSG){
                            Console.WriteLine($"{displayname}: {message}");
                        }
                        else if(UDPmessageHelper.getMessageType(response) == MessageType.ERR){
                            Console.Error.WriteLine($"ERR FROM {displayname}: {message}");
                            Disconnect();
                        }
                        continue;
                    }
                    if(UDPmessageHelper.getRefMessageID(response) != messageID)
                    {
                        continue;
                    }
                    if(UDPmessageHelper.getReplyResult(response) == 1)
                    {
                        Console.Error.WriteLine($"Success: {UDPmessageHelper.getReplyMessageContents(response)}");
                        return true;
                    }
                    else if(UDPmessageHelper.getReplyResult(response) == 0)
                    {
                        Console.Error.WriteLine($"Failure: {UDPmessageHelper.getReplyMessageContents(response)}");
                        return false;
                    }
                }
                catch (SocketException e)
                {
                    if (e.SocketErrorCode == SocketError.TimedOut)
                    {
                        Console.WriteLine("Timeout: Server did not reply.");
                        continue;
                    }
                }
                // catch (Exception e)
                // {
                //     Console.Error.WriteLine($"Exception: {e.Message}");
                // }              
            }

            Console.WriteLine("Failure: Server did not reply.");
            return false;
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