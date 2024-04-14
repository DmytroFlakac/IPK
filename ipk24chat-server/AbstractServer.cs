using System;
using System.Collections.Generic;
using System.Threading;
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
        
        public virtual Task Start(CancellationToken cts)
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

       
        public virtual Task HandleClientAsync(User user, CancellationToken cts)
        {
            throw new NotImplementedException("HandleClientAsync not implemented");
        }
        
        public virtual Task HandleClientAsync(User user, byte[] message, CancellationToken cts)
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
        
        public virtual void HandleJoin(User user, byte[] message)
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
        
        public virtual void HandleBye(User user, byte[] message)
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
        
        public virtual void HandleERR_FROM(User user, byte[] message)
        {
            throw new NotImplementedException("HandleERR_FROM not implemented");
        }
        
        public bool ExistedUser(User user)
        {
            lock (ClientsLock)
            {
                foreach (var channel in Channels)
                {
                    foreach (var u in channel.Value)
                    {
                        if (u.Username == user.Username && u.IsAuthenticated)
                        {
                            return true;
                        }
                    }
                }
                
            }
            return false;
        }
        
        public void AddUser(User user, string channelId)
        {
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
        }
        
        public virtual void CleanUser(User user)
        {
            throw new NotImplementedException("CleanUser not implemented");
        }
        
        public async Task BroadcastMessage(string message, User? sender, string channelId = "default")
        {
            List<User> usersToMessage = new List<User>();
            lock (ClientsLock)
            {
                if (Channels.TryGetValue(channelId, out var users))
                {
                    usersToMessage = new List<User>(users);
                }
            }
            
            List<Task> tasks = new List<Task>();
            foreach (User user in usersToMessage)
            {
                if (user == sender || !user.IsAuthenticated)
                    continue;
                tasks.Add(user.WriteAsync(message));
            }
            await Task.WhenAll(tasks);
        }
    }
}