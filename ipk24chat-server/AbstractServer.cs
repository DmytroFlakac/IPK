using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Server
{
    public abstract class AbstractServer : IServer
    {
        protected string Host;
        protected int Port;
        protected Dictionary<string, List<User>> Channels;
        protected readonly object ClientsLock = new object();

        protected string ChannelId;
        
        protected AbstractServer(string host, int port, Dictionary<string, List<User>> channels, string channelId = "default")
        {
            Host = host;
            Port = port;
            Channels = channels ?? throw new ArgumentNullException(nameof(channels), "Channels list cannot be null.");
            ChannelId = channelId;
        }
        
        public virtual Task Start()
        {
            throw new NotImplementedException("Start not implemented");
        }
        public virtual void Stop()
        {
            throw new NotImplementedException("Stop not implemented");
        }
        
        public virtual Task AcceptClientsAsync()
        {
            throw new NotImplementedException("AcceptClientsAsync not implemented");
        }

        // public virtual Task SendMessage(TcpClient client, string message)
        // {
        //     throw new NotImplementedException("SendTCPMessage not implemented");
        // }
        //
        // public virtual Task SendMessage(IPEndPoint endPoint, string message)
        // {
        //     throw new NotImplementedException("SendUDPMessage not implemented");
        // }

        // public virtual string ReceiveMessage(object client)
        // {
        //     throw new NotImplementedException("ReceiveMessage not implemented");
        // }
        //
        
        public virtual Task HandleClientAsync(User user)
        {
            throw new NotImplementedException("HandleClientAsync not implemented");
        }
        
        public virtual Task HandleClientAsync(User user, byte[] message)
        {
            throw new NotImplementedException("HandleClientAsync not implemented");
        }
        
        public virtual void HandleAuth(User user, string message)
        {
            throw new NotImplementedException("HandleAuth not implemented");
        }
        
        public virtual void HandleAuth(User user, byte[] message)
        {
            throw new NotImplementedException("HandleAuth not implemented");
        }
        
        public virtual void HandleJoin(User user, string message)
        {
            throw new NotImplementedException("HandleJoin not implemented");
        }
        
        public virtual void HandleMessage(User user, string message)
        {
            throw new NotImplementedException("HandleMessage not implemented");
        }
        
        public virtual void HandleMessage(User user, byte[] message)
        {
            throw new NotImplementedException("HandleMessage not implemented");
        }
        
        public virtual void HandleBye(User user)
        {
            throw new NotImplementedException("HandleBye not implemented");
        }
        
        public virtual bool CheckAuth(User user, string message)
        {
            throw new NotImplementedException("CheckAuth not implemented");
        }
        
        public virtual bool CheckMessage(User user, string message)
        {
            throw new NotImplementedException("CheckMessage not implemented");
        }
        
        public virtual void HandleERR_FROM(User user, string message)
        {
            throw new NotImplementedException("HandleERR_FROM not implemented");
        }
        
        public bool ExistedUser(User user)
        {
            lock (ClientsLock)
            {
                foreach (var u in Channels[ChannelId])
                {
                    if (u.Username == user.Username && u.IsAuthenticated)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        
        
        public async Task BroadcastMessage(string message, User? sender, string channelId = "default")
        {
            List<Task> tasks = new List<Task>();
            lock (ClientsLock)
            {
                foreach (User user in Channels[channelId])
                {
                    if (user == sender || !user.IsAuthenticated) continue;
                    Task sendTask = user.WriteAsync(message);
                    tasks.Add(sendTask);
                }
            }
            await Task.WhenAll(tasks);
        }
    }
}