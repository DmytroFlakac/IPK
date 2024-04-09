using System.Net;
using System.Net.Sockets;

namespace Server
{
    public abstract class AbstractServer : IServer
    {
        protected string Host;
        protected int Port;
        protected List<IUser> Users;
        
        protected AbstractServer(string host, int port, List<IUser> users)
        {
            Host = host;
            Port = port;
            Users = users ?? throw new ArgumentNullException(nameof(users), "Users list cannot be null.");
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