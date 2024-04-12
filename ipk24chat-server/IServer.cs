using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Server
{
    public interface IServer
    {
        // Starts the server and begins listening for client connections
        Task Start(CancellationToken cts);

        // Stops the server and cleans up resources
        void Stop();

        // Handles new client connections
        // void AcceptClients(object client);
        Task AcceptClientsAsync();
        
        Task HandleClientAsync(User user, CancellationToken cts);
        
        void HandleAuth(User user, string message);
        
        void HandleAuth(User user, byte[] message);
        
        void HandleJoin(User user, string message);
        
        void HandleMessage(User user, string message);

        void HandleBye(User user);
        
        bool CheckAuth(User user, string message);
        
        bool CheckMessage(User user, string message);
        
        public void HandleERR_FROM(User user, string message);

        // Sends a message to a specified client
        // Task SendMessage(TcpClient client, string message);
        // Task SendMessage(IPEndPoint endPoint, string message);

        // Receives a message from a specified client
        // string ReceiveMessage(object client);
        
        void AddUser(User user, string channelId);

        Task BroadcastMessage(string message, User sender, string channelId = "default");
        
        
    }
}
