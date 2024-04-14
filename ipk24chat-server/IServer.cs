using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    public interface IServer
    {
        Task Start(CancellationToken cts);
        
        void Stop();
        Task AcceptClientsAsync();
        
        Task HandleClientAsync(User user, CancellationToken cts);
        
        Task HandleClientAsync(User user, byte[] message, CancellationToken cts);
        
        void HandleAuth(User user, string message);
        
        void HandleAuth(User user, byte[] message);
        
        void HandleJoin(User user, string message);
        
        void HandleJoin(User user, byte[] message);
        
        void HandleMessage(User user, string message);
        
        void HandleMessage(User user, byte[] message);

        void HandleBye(User user);

        void HandleBye(User user, byte[] message);
        bool CheckAuth(User user, string message);
        
        
        bool CheckMessage(User user, string message);
        
        public void HandleERR_FROM(User user, string message);

        void HandleERR_FROM(User user, byte[] message);

        bool ExistedUser(User user);
        
        void AddUser(User user, string channelId);
        
        void CleanUser(User user);

        Task BroadcastMessage(string message, User sender, string channelId = "default");
        
        
    }
}
