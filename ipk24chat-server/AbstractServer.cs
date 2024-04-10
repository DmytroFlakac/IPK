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
        protected Dictionary<string, List<User>> Channels = new Dictionary<string, List<User>>();

        protected string ChannelId = "default";
        
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
        // public virtual void AcceptClients(object client)
        // {
        //     throw new NotImplementedException("AcceptClients not implemented");
        // }
        
        public virtual void AcceptClients()
        {
            throw new NotImplementedException("AcceptClients not implemented");
        }

        public virtual Task SendMessage(TcpClient client, string message)
        {
            throw new NotImplementedException("SendTCPMessage not implemented");
        }

        public virtual Task SendMessage(IPEndPoint endPoint, string message)
        {
            throw new NotImplementedException("SendUDPMessage not implemented");
        }

        public virtual string ReceiveMessage(object client)
        {
            throw new NotImplementedException("ReceiveMessage not implemented");
        }
        
        public virtual Task BroadcastMessage(string message)
        {
            throw new NotImplementedException("BroadcastMessage not implemented");
        }
    }
}