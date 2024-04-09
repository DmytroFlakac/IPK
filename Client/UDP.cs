using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace IPK24Chat
{
    class UdpChatClient{
        private UdpClient client;
        private string serverAddress;
        private int serverPort;
        private int udpRetryCount;
        private string displayName;
        private bool autorized = false;
        private int messageID = -1;
        private string displayRegex = @"^[!-~]{1,20}$";
        private string messageRegex =  @"^[\r\n -~]{1,1400}$";
        private static string baseRegex = @"^[a-zA-Z0-9-]+$";
        

        public UdpChatClient(string serverAddress, int serverPort, int udpConfirmationTimeout, int udpRetryCount, UdpClient client)
        {
            this.client = client;
            this.serverAddress = serverAddress;
            this.serverPort = serverPort;
            client.Client.ReceiveTimeout = udpConfirmationTimeout;
            client.Client.SendTimeout = udpConfirmationTimeout;
            this.udpRetryCount = udpRetryCount;
        }

        public async Task ListenForServer(UdpClient client, CancellationToken cts)
        {
            
            try
            {
                while (!cts.IsCancellationRequested)
                {                    
                    IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.Any, 0);
                    UdpReceiveResult result = await client.ReceiveAsync(cts).ConfigureAwait(false);
                    byte[] message = result.Buffer;
                    serverEndpoint = result.RemoteEndPoint;
                    int messageID = UDPmessageHelper.getMessageID(message);
                    MessageType messageType = UDPmessageHelper.getMessageType(message);
                    if(messageType == MessageType.MSG)
                    {
                        sendConfirmation(messageID);
                        string msg = UDPmessageHelper.getMSGContents(message);
                        string displayname = UDPmessageHelper.getMSGdisplayName(message);
                        if(!Regex.IsMatch(msg, messageRegex) || !Regex.IsMatch(displayname, displayRegex))
                        {
                            ++messageID;
                            Console.Error.WriteLine("ERR: Message must be alphanumeric and display name must be between 1 and 20 characters.");
                            byte[] errorMessage = UDPmessageHelper.buildErrorMessage(messageID, "Message must be alphanumeric and display name must be between 1 and 20 characters.", displayName, MessageType.ERR);
                            client.Send(errorMessage, errorMessage.Length, serverEndpoint);
                            waitConfirmation(errorMessage);    
                            Disconnect();
                        }
                        Console.WriteLine($"{displayname}: {msg}");
                    }
                    else if(messageType == MessageType.ERR)
                    {
                        sendConfirmation(messageID);
                        string msg = UDPmessageHelper.getMSGContents(message);
                        string displayname = UDPmessageHelper.getMSGdisplayName(message);
                        if(!Regex.IsMatch(msg, messageRegex) || !Regex.IsMatch(displayname, displayRegex))
                        {
                            ++messageID;
                            Console.Error.WriteLine("ERR: Message must be alphanumeric and display name must be between 1 and 20 characters.");
                            byte[] errorMessage = UDPmessageHelper.buildErrorMessage(messageID, "Message must be alphanumeric and display name must be between 1 and 20 characters.", displayName, MessageType.ERR);
                            client.Send(errorMessage, errorMessage.Length, serverEndpoint);
                            waitConfirmation(errorMessage);    
                            Disconnect();
                        }
                        Console.Error.WriteLine($"ERR FROM {displayname}: {msg}");
                        Disconnect();
                    } 
                    else if(messageType == MessageType.BYE)
                    {
                        sendConfirmation(messageID);
                        if(cts.IsCancellationRequested)
                            cts.ThrowIfCancellationRequested();
                        client.Close();
                        Environment.Exit(0);
                    } 
                    else if(messageType == MessageType.CONFIRM)
                    {
                        continue;   
                    }  
                    else
                    {
                        sendConfirmation(messageID);
                        Console.Error.WriteLine("ERR: Unexpected message type.");
                        byte[] errorMessage = UDPmessageHelper.buildErrorMessage(messageID, "Unexpected message type.", displayName, MessageType.ERR);
                        ++messageID;
                        client.Send(errorMessage, errorMessage.Length, serverEndpoint);
                        waitConfirmation(errorMessage);
                        Disconnect();
                    }     
                }
            }
            catch (SocketException e)
            {
                Console.Error.WriteLine($"ERR: {e.Message}");
            }
        }

        public void StartInteractiveSession()
        {
            Console.CancelKeyPress += (sender, e) => {           
                    Disconnect();  
                    e.Cancel = true;          
                };
            while (true)
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                Task listenTask = ListenForServer(client, cts.Token);
                Thread.Sleep(100);
                string input = Console.ReadLine();
                cts.Cancel();

                if (string.IsNullOrEmpty(input))
                {
                    if(!cts.IsCancellationRequested)
                        cts.Token.ThrowIfCancellationRequested();
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
                    else if (input.Length > 1400 || !Regex.IsMatch(input, messageRegex))
                    {
                        Console.Error.WriteLine("ERR: Message too long. Max length is 1400 characters. Message must be alphanumeric");
                        continue;
                    }
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
            Thread.Sleep(100);
        }


        private void Disconnect()
        {
            try {
                ++messageID;
                byte[] messageBytes = UDPmessageHelper.buildBYEMessage(messageID, MessageType.BYE);
                client.Send(messageBytes, messageBytes.Length, serverAddress, serverPort);
                waitConfirmation(messageBytes);
                client.Close();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"ERR: {e.Message}");
            }
            finally
            {
                Environment.Exit(0);
            }
        }

        private void HandleAuth(string username, string secret, string displayName)
        {
            if(!Regex.IsMatch(displayName, displayRegex) || !Regex.IsMatch(username, baseRegex) 
            || !Regex.IsMatch(secret, baseRegex) || secret.Length > 120 || username.Length > 20)
            {
                if(!Regex.IsMatch(secret, baseRegex))
                    Console.Error.WriteLine("ERR: Secret must be alphanumeric and between 1 and 120 characters.");
                Console.Error.WriteLine("ERR: Display name must be alphanumeric and between 1 and 20 characters or username and secret must be alphanumeric.");
            }
            ++messageID;
            byte[] messageBytes = UDPmessageHelper.buildMessage(username, secret, displayName, messageID, MessageType.AUTH);
            client.Send(messageBytes, messageBytes.Length, serverAddress, serverPort);
            this.displayName = displayName;
            if(!waitConfirmation(messageBytes))            
                return;   
            if(waitReply())
                autorized = true;
        }

        private void HandleJoin(string channelID)
        {
            if(!Regex.IsMatch(channelID, baseRegex) || channelID.Length > 20)
            {
                Console.Error.WriteLine("ERR: Channel ID must be alphanumeric and between 1 and 20 characters.");
                return;
            }
            ++messageID;
            byte[] messageBytes = UDPmessageHelper.buildMessage(channelID, messageID, displayName, MessageType.JOIN);
            client.Send(messageBytes, messageBytes.Length, serverAddress, serverPort);
            if(!waitConfirmation(messageBytes))            
                return;
            waitReply();
        }

        private void HandleMessage(string message)
        {
            if(!Regex.IsMatch(message, messageRegex))
            {
                Console.Error.WriteLine("ERR: Message must be alphanumeric and between 1 and 1400 characters.");
                return;
            }
            ++messageID;
            byte[] messageBytes = UDPmessageHelper.buildMessage(messageID, message, displayName, MessageType.MSG);
            client.Send(messageBytes, messageBytes.Length, serverAddress, serverPort);
            if(!waitConfirmation(messageBytes))            
                return;
        }

        public void sendConfirmation(int messageID)
        {
            byte[] messageBytes = UDPmessageHelper.buildMessage(messageID, MessageType.CONFIRM);
            client.Send(messageBytes, messageBytes.Length, serverAddress, serverPort);
        }

        private bool waitConfirmation(byte[] messageBytes)
        {
            for (int i = 0; i < udpRetryCount; i++)
            {
                try
                {
                    IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.Any, 0);
                    byte[] response = client.Receive(ref serverEndpoint);
                    if (UDPmessageHelper.getMessageType(response) == MessageType.CONFIRM && 
                    UDPmessageHelper.getMessageID(response) == messageID)
                    {
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
                        continue;
                    }
                }
            }
            return false;
        }

        private bool waitReply()
        {
            for (int i = 0; i <= udpRetryCount; i++)
            {
                try
                {
                    IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.Any, 0); 
                    byte[] response = client.Receive(ref serverEndpoint);
                    serverPort = serverEndpoint.Port;
                    int responseID = UDPmessageHelper.getMessageID(response);
                    sendConfirmation(responseID);
                    if(UDPmessageHelper.getMessageType(response) != MessageType.REPLY)
                    {
                        if(UDPmessageHelper.getMessageType(response) == MessageType.CONFIRM)
                            continue;
                        string message = UDPmessageHelper.getMSGContents(response);
                        string displayname = UDPmessageHelper.getMSGdisplayName(response);
                        if(!Regex.IsMatch(message, messageRegex) || !Regex.IsMatch(displayname, displayRegex))
                        {
                            Console.Error.WriteLine("ERR: Message must be alphanumeric and display name must be between 1 and 20 characters.");
                            byte[] errorMessage = UDPmessageHelper.buildErrorMessage(messageID, "Message must be alphanumeric and display name must be between 1 and 20 characters.", displayName, MessageType.ERR);
                            client.Send(errorMessage, errorMessage.Length, serverEndpoint);
                            waitConfirmation(errorMessage);    
                            Disconnect();
                        }
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
                        continue;
                    }
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"Exception: {e.Message}");
                }              
            }
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