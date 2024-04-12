using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Server;

public class TcpServer : AbstractServer
{
    private readonly TcpListener _server;

    public TcpServer(string ipAddress, int port, Dictionary<string, List<User>> channels, string channelId = "default") : 
        base(ipAddress, port, channels, channelId)
    {
        _server = new TcpListener(IPAddress.Parse(ipAddress), port);
    }

    

    public override async Task Start(CancellationToken cts)
    {
        _server.Start();
        var tasks = new List<Task>();
        try
        {
            while (!cts.IsCancellationRequested)
            {
                var client = await _server.AcceptTcpClientAsync(cts);  
                TcpUser user = new TcpUser(client);

                lock (ClientsLock)
                {
                    if (!Channels.ContainsKey(ChannelId))
                    {
                        Channels.Add(ChannelId, new List<User>());
                    }
                    Channels[ChannelId].Add(user);
                }
                var clientTask = HandleClientAsync(user, cts); // Store the task
                tasks.Add(clientTask);
            }
        }
        catch (OperationCanceledException)
        {
            // Ignore
        }
        finally
        {
            _server.Stop();
            await Task.WhenAll(tasks); // Ensure all tasks complete or are cancelled
        }
    }

    
    public override async Task HandleClientAsync(User user, CancellationToken cts)
    { 
        try
        {
            while (!cts.IsCancellationRequested)
            {
                string? message = await user.ReadAsyncTcp(cts);
               
                if (message == null) 
                {
                    Console.WriteLine($"SENT {user.UserServerPort()} | BYE");
                    await user.WriteAsync("BYE");
                    user.Disconnect();
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
                        await user.WriteAsync("ERR FROM Server IS Invalid message format");
                        HandleBye(user);
                        break;
                }
            }
        }
        catch (IOException) // Catch exceptions when client disconnects unexpectedly
        {
            if (user.IsConnected())
            {
                Console.WriteLine($"SENT {user.UserServerPort()} | BYE BYE");
                await user.WriteAsync("BYE");
                user.Disconnect();
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"SENT {user.UserServerPort()} | BYE BYE");
            await user.WriteAsync("BYE");
            user.Disconnect();
        }
    }
    
    
    public override async void HandleAuth(User user, string message)
    {
        Console.WriteLine($"RECV {user.UserServerPort()} | AUTH {message}");
        var parts = message.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        user.SetUsername(parts[1]);
        user.SetDisplayName(parts[3]);
        if (CheckAuth(user, message))
        {
            
            user.SetAuthenticated();
            await user.WriteAsync("REPLY OK IS Authenticated successfully");
            await BroadcastMessage($"MSG FROM Server IS {user.DisplayName} has joined {user.ChannelId}", null);
        }
    }
    
    public override async void HandleJoin(User user, string message)
    {
        Console.WriteLine($"RECV {user.UserServerPort()} | JOIN {message}");
        var match = Regex.Match(message, user.JoinRegex, RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            await user.WriteAsync("REPLY NOK IS Invalid join format");
            return;
        }
        user.SetDisplayName(match.Groups[2].Value);
        await BroadcastMessage($"MSG FROM Server IS {user.DisplayName} has left {user.ChannelId}", user, user.ChannelId);
        var channelId = match.Groups[1].Value;
        AddUser(user, channelId);
        await BroadcastMessage($"MSG FROM Server IS {user.DisplayName} has joined {channelId}", null, channelId);
        await user.WriteAsync($"REPLY OK IS Joined {channelId}");
       
    }

    
    public override bool CheckAuth(User user, string message)
    {
        var parts = message.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 6 || parts[0].ToUpper() != "AUTH" || parts[4].ToUpper() != "USING" || 
            !Regex.IsMatch(parts[3], user.DisplayRegex) || !Regex.IsMatch(parts[5], user.BaseRegex))
        {
            user.WriteAsync("REPLY NOK IS Invalid auth format");
            return false;
        }

        if (ExistedUser(user))
        {
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
            await user.WriteAsync("ERR FROM Server IS Invalid message format");
            HandleBye(user);
        }
        // Console.WriteLine("Broadcast in HandleMessage");
        await BroadcastMessage(message, user, user.ChannelId);
    }
    
    public override bool CheckMessage(User user, string message)
    {
        if (!Regex.IsMatch(message, user.MSGERRRegex))
        {
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
    
    public override async void HandleBye(User user)
    {
        Console.WriteLine($"RECV {user.UserServerPort()} | BYE");
        lock (ClientsLock)
        {
            Channels[user.ChannelId].Remove(user);
        }
        user.Disconnect();
        await BroadcastMessage($"MSG FROM Server IS {user.DisplayName} has left {user.ChannelId}", user, user.ChannelId);
    }
}

