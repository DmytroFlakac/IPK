using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    public class TcpServer : AbstractServer
    {
        private readonly TcpListener _server;

        public TcpServer(string ipAddress, int port, Dictionary<string, List<User>> channels, string channelId = "default") : 
            base(ipAddress, port, channels, channelId)
        {
            _server = new TcpListener(IPAddress.Parse(ipAddress), port);
        }

        public override async Task Start()
        {
            _server.Start();
            await AcceptClientsAsync(); // Use asynchronous method to accept clients
        }

        public override async Task AcceptClientsAsync()
        {
            while (true)
            {
                var client = await _server.AcceptTcpClientAsync();
                TcpUser user = new TcpUser(client); 
                
                lock (ClientsLock)
                {
                    if (!Channels.ContainsKey(ChannelId))
                    {
                        Channels.Add(ChannelId, new List<User>());
                    }
                    Channels[ChannelId].Add(user);
                }
                _ = HandleClientAsync(user); // Handle client asynchronously
            }
        }
        
        public override async Task HandleClientAsync(User user)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    string? message = await user.ReadAsyncTcp();
                   
                    if (message == null) 
                    {
                        cts.Cancel();
                    }
                    var messageType = user.GetMessageType(message);
                    switch (messageType)
                    {
                        case User.MessageType.AUTH:
                            HandleAuth(user, message);
                            break;
                        case User.MessageType.JOIN:
                            HandleJoin(user, message);
                            break;
                        case User.MessageType.MSG:
                            HandleMessage(user, message);
                            break;
                        case User.MessageType.ERR:
                            HandleERR_FROM(user, message);
                            break;
                        case User.MessageType.BYE:
                            HandleBye(user);
                            break;
                        default:
                            Console.WriteLine($"SENT {user.UserServerPort()} | ERR Invalid message format");
                            await user.WriteAsync("ERR FROM Server IS Invalid message format");
                            HandleBye(user);
                            break;
                    }
                }
            }
            catch (IOException) // Catch exceptions when client disconnects unexpectedly
            {
                HandleBye(user);        
            }
            finally
            {
                cts.Cancel();
            }
        }
        
        
        public override void HandleAuth(User user, string message)
        {
            Console.WriteLine($"RECV {user.UserServerPort()} | AUTH {message}");
            if (CheckAuth(user, message))
            {
                var parts = message.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                user.SetUsername(parts[1]);
                user.SetDisplayName(parts[3]);
                user.SetAuthenticated();
                Console.WriteLine($"SENT {user.UserServerPort()} | REPLY Authenticated successfully");
                user.WriteAsync("REPLY OK IS Authenticated successfully");
                Console.WriteLine($"SENT {user.UserServerPort()} | MSG {user.DisplayName} has joined {user.ChannelId}");
                BroadcastMessage($"MSG FROM Server IS {user.DisplayName} has joined {user.ChannelId}", null);
            }
        }
        
        public override void HandleJoin(User user, string message)
        {
            Console.WriteLine($"RECV {user.UserServerPort()} | JOIN {message}");
            var match = Regex.Match(message, user.JoinRegex, RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                Console.WriteLine($"SENT {user.UserServerPort()} | REPLY invalid join format");
                user.WriteAsync("REPLY NOK IS Invalid join format");
                return;
            }
            user.SetDisplayName(match.Groups[2].Value);
            BroadcastMessage($"MSG FROM Server IS {user.DisplayName} has left {user.ChannelId}\r\n", user, user.ChannelId);
            var channelId = match.Groups[1].Value;
            

            lock (ClientsLock)
            {
                if (!string.IsNullOrEmpty(user.ChannelId) && Channels.ContainsKey(user.ChannelId))
                {
                    Channels[user.ChannelId].Remove(user);
                }
                user.ChannelId = channelId;
                if (!Channels.ContainsKey(channelId))
                {
                    Channels[channelId] = new List<User>();
                }
                Channels[channelId].Add(user);
            }
            Console.WriteLine($"SENT {user.UserServerPort()} | MSG {user.DisplayName} has joined {channelId}");
            BroadcastMessage($"MSG FROM Server IS {user.DisplayName} has joined {channelId}", null, channelId);
            Console.WriteLine($"SENT {user.UserServerPort()} | REPLY Joined {channelId}");
            user.WriteAsync($"REPLY OK IS Joined {channelId}");
        }

        
        public override bool CheckAuth(User user, string message)
        {
            var parts = message.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 6 || parts[0].ToUpper() != "AUTH" || parts[4].ToUpper() != "USING" || 
                !Regex.IsMatch(parts[3], user.DisplayRegex) || !Regex.IsMatch(parts[5], user.BaseRegex))
            {
                Console.WriteLine($"SENT {user.UserServerPort()} | REPLY invalid auth format");
                user.WriteAsync("REPLY NOK IS Invalid auth format");
                return false;
            }

            if (ExistedUser(user))
            {
                Console.WriteLine($"SENT {user.UserServerPort()} | REPLY NOK IS User already connected");
                user.WriteAsync("REPLY NOK IS User already connected");
                return false;
            }
            
            return true;
        }
        
        
        public override async void HandleMessage(User user, string message)
        {
            Console.WriteLine($"RECV {user.UserServerPort()} | MSG {message}");
            if (!CheckMessage(user, message))
            {
                Console.WriteLine($"SENT {user.UserServerPort()} | ERR Invalid message format");
                await user.WriteAsync("ERR FROM Server IS Invalid message format");
                HandleBye(user);
            }
            Console.WriteLine("Broadcast in HandleMessage");
            await BroadcastMessage(message, user, user.ChannelId);
        }
        
        public override bool CheckMessage(User user, string message)
        {
            if (!Regex.IsMatch(message, user.MessageRegex))
            {
                Console.WriteLine($"SENT {user.UserServerPort()} | REPLY invalid message format");
                user.WriteAsync("REPLY NOK IS Invalid message format");
                return false;
            }
            return true;
        }
        
        public override void HandleERR_FROM(User user, string message)
        {
            if (!CheckMessage(user, message))
            {
                Console.WriteLine($"RECV {user.UserServerPort()} | ERR {message}");
                HandleBye(user);
            }
        }
        
        public override void HandleBye(User user)
        {
            Console.WriteLine($"RECV {user.UserServerPort()} | BYE");
            lock (ClientsLock)
            {
                Channels[user.ChannelId].Remove(user);
            }
            Console.WriteLine($"SENT {user.UserServerPort()} | MSG {user.DisplayName} has left {user.ChannelId}");
            user.Disconnect();
            BroadcastMessage($"MSG FROM Server IS {user.DisplayName} has left {user.ChannelId}", user, user.ChannelId);
        }

        
        
        
        
    }
    
   
}
