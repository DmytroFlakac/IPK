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
        string? message = null;
        try
        {
            while (!cts.IsCancellationRequested && user.Active)
            {
                CancellationTokenSource cts2 = new CancellationTokenSource();
                cts.Register(() => cts2.Cancel());
                
                if (!cts2.IsCancellationRequested)
                    message = await user.ReadAsyncTcp(cts2.Token);
               
                if (message == null) 
                {
                    CleanUser(user);
                    cts2.Cancel();
                    break;
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
                        cts2.Cancel();
                        break;
                    default:
                        await user.WriteAsync("ERR FROM Server IS Invalid message format");
                        HandleBye(user);
                        cts2.Cancel();
                        break;
                }
            }
        }
        catch (IOException) 
        {
            CleanUser(user);
        }
        catch (OperationCanceledException)
        {
            CleanUser(user);
        }
    }
    
    
    public override async void HandleAuth(User user, string message)
    {
        Console.WriteLine($"RECV {user.UserServerPort()} | AUTH {message}");
        if (CheckAuth(user, message))
        {
            var parts = message.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            user.SetUsername(parts[1]);
            user.SetDisplayName(parts[3]);
            user.SetAuthenticated();
            await user.WriteAsync("REPLY OK IS Authenticated successfully");
            AddUser(user, "default");
            await user.WriteAsync($"MSG FROM Server IS {user.DisplayName} has joined {user.ChannelId}");
            var broadcast = BroadcastMessage($"MSG FROM Server IS {user.DisplayName} has joined {user.ChannelId}", user);
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
        var channelId = match.Groups[1].Value;
        
        var broadcast = BroadcastMessage($"MSG FROM Server IS {user.DisplayName} has left {user.ChannelId}", user, user.ChannelId);
        await user.WriteAsync($"MSG FROM Server IS {user.DisplayName} has joined {channelId}");
        await user.WriteAsync($"REPLY OK IS Joined {channelId}");
        AddUser(user, channelId);
        var broadcast2 = BroadcastMessage($"MSG FROM Server IS {user.DisplayName} has joined {channelId}", user, channelId);
        
       
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
            CleanUser(user);
            return;
        }
        // Console.WriteLine("Broadcast in HandleMessage");
        user.SetDisplayName(message.Split(" ")[2]);
        var broadcast = BroadcastMessage(message, user, user.ChannelId);
    }
    
    public override bool CheckMessage(User user, string message)
    {
        if (!Regex.IsMatch(message, user.MSGERRRegex))
            return false;
        return true;
    }
    
    public override void HandleERR_FROM(User user, string message)
    {
        if (CheckMessage(user, message))
        {
            Console.WriteLine($"RECV {user.UserServerPort()} | ERR {message}");
            user.SetDisplayName(message.Split(" ")[2]);
            CleanUser(user);
        }
        else
        {
            user.WriteAsync("ERR FROM Server IS Invalid message format");
            CleanUser(user);
        }
    }
    
    public override void HandleBye(User user)
    {
        Console.WriteLine($"RECV {user.UserServerPort()} | BYE");
        lock (ClientsLock)
        {
            Channels[user.ChannelId].Remove(user);
        }
        var broadcast = BroadcastMessage($"MSG FROM Server IS {user.DisplayName} has left {user.ChannelId}", user, user.ChannelId);
        user.Disconnect();
    }
    
    public override async void CleanUser(User user)
    {
        try
        {
            lock (ClientsLock) Channels[user.ChannelId].Remove(user);
            await user.WriteAsync("BYE");
            var broadcast =  BroadcastMessage($"MSG FROM Server IS {user.DisplayName} has left {user.ChannelId}", user, user.ChannelId);
            user.Disconnect();
        }
        catch (Exception)
        {
            // Ignore
        }
    }
}

