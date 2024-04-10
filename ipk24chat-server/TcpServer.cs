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
        private readonly object _clientsLock = new object();

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

        private async Task AcceptClientsAsync()
        {
            while (true)
            {
                var client = await _server.AcceptTcpClientAsync();
                TcpUser user = new TcpUser(client); 
                
                lock (_clientsLock)
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

        private async Task HandleClientAsync(User user)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    string? message = await user.ReadAsync(); // Read line asynchronously
                    if (message == null) // Client has disconnected gracefully
                    {
                        break;
                    }
                    else if (message.Contains("AUTH"))
                    {
                        HandleAuth(user, message);
                    }
                    else if(message.Contains("JOIN"))
                    {
                        HandleJoin(user, message);
                    }
                    else if(message.Contains("MSG FROM"))
                    {
                        Console.WriteLine($"RECV {user.UserServerPort()} | MSG {message}");
                        await BroadcastMessage(message, user, user.ChannelId);
                    }
                    else if(message.Contains("ERR FROM"))
                    {
                        HandleERR_FROM(user, message);
                    }
                    else if (message == "BYE")
                    {
                        HandleBye(user);
                        break;
                    }
                    else
                    {
                        Console.WriteLine($"RECV: {message}");
                        await BroadcastMessage(message, user, user.ChannelId);
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
        
        public void HandleAuth(User user, string message)
        {
            Console.WriteLine($"RECV {user.UserServerPort()} | AUTH {message}");
            if (CheckAuth(user, message))
            {
                var parts = message.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                user.SetUsername(parts[1]);
                user.SetDisplayName(parts[3]);
                user.SetAuthenticated();
                Console.WriteLine($"SENT {user.UserServerPort()} | REPLY Authenticated successfully");
                user.WriteAsync("REPLY OK IS Authenticated successfully\r\n");
                Console.WriteLine($"SENT {user.UserServerPort()} | MSG {user.DisplayName} has joined {user.ChannelId}");
                BroadcastMessage($"MSG FROM Server IS {user.DisplayName} has joined {user.ChannelId}\r\n", null);
            }
        }
        
        public void HandleJoin(User user, string message)
        {
            Console.WriteLine($"RECV {user.UserServerPort()} | JOIN {message}");
            var match = Regex.Match(message, user.JoinRegex, RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                Console.WriteLine($"SENT {user.UserServerPort()} | REPLY invalid join format");
                user.WriteAsync("REPLY NOK IS Invalid join format\r\n");
                return;
            }
            BroadcastMessage($"MSG FROM Server IS {user.DisplayName} has left {user.ChannelId}\r\n", user, user.ChannelId);
            var channelId = match.Groups[1].Value;

            lock (_clientsLock)
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
            BroadcastMessage($"MSG FROM Server IS {user.DisplayName} has joined {channelId}\r\n", null, channelId);
            Console.WriteLine($"SENT {user.UserServerPort()} | REPLY Joined {channelId}");
            user.WriteAsync($"REPLY OK IS Joined {channelId}\r\n");
        }

        
        public bool CheckAuth(User user, string message)
        {
            var parts = message.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 6 || parts[0].ToUpper() != "AUTH" || parts[4].ToUpper() != "USING" || 
                !Regex.IsMatch(parts[3], user.DisplayRegex) || !Regex.IsMatch(parts[5], user.BaseRegex))
            {
                Console.WriteLine($"SENT {user.UserServerPort()} | REPLY invalid auth format");
                user.WriteAsync("REPLY NOK IS Invalid auth format\r\n");
                return false;
            }  
            return true;
        }
        
        public bool CheckMessage(User user, string message)
        {
            if (!Regex.IsMatch(message, user.MessageRegex))
            {
                Console.WriteLine($"SENT {user.UserServerPort()} | REPLY invalid message format");
                user.WriteAsync("REPLY NOK IS Invalid message format\r\n");
                return false;
            }
            return true;
        }
        
        public void HandleERR_FROM(User user, string message)
        {
            if (!CheckMessage(user, message))
            {
                Console.WriteLine($"RECV {user.UserServerPort()} | ERR {message}");
                HandleBye(user);
            }
        }
        
        
        public void HandleBye(User user)
        {
            Console.WriteLine($"RECV {user.UserServerPort()} | BYE");
            lock (_clientsLock)
            {
                Channels[user.ChannelId].Remove(user);
            }
            Console.WriteLine($"SENT {user.UserServerPort()} | MSG {user.DisplayName} has left {user.ChannelId}");
            user.Disconnect();
            BroadcastMessage($"MSG FROM Server IS {user.DisplayName} has left {user.ChannelId}\r\n", user, user.ChannelId);
        }

        private Task BroadcastMessage(string message, User sender, string channelId = "default")
        {
            lock (_clientsLock)
            {
                foreach (var user in Channels[channelId])
                {
                    if (user == sender || !message.Contains("MSG FROM") || !user.IsAuthenticated) continue;
                    if (!CheckMessage(user, message))
                    {
                        Console.WriteLine($"SENT {user.UserServerPort()} | ERR Invalid message format");
                        user.WriteAsync("ERR FROM Server IS Invalid message format\r\n");
                        HandleBye(user);
                    }
                    user.WriteAsync(message);
                }
            }
            return Task.CompletedTask;
        }
    }
    
   
}
