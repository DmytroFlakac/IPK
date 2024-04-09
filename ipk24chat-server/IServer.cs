using System.Net;
using System.Net.Sockets;

namespace Server
{
    public interface IServer
    {
        // Starts the server and begins listening for client connections
        Task Start();

        // Stops the server and cleans up resources
        void Stop();

        // Handles new client connections
        // void AcceptClients(object client);
        void AcceptClients();

        // Sends a message to a specified client
        Task SendMessage(TcpClient client, string message);
        Task SendMessage(IPEndPoint endPoint, string message);

        // Receives a message from a specified client
        string ReceiveMessage(object client);
        
        Task BroadcastMessage(string message);
    }
}
