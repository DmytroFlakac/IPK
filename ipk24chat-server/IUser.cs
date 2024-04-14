using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    public interface IUser
    {
        // Properties
        string Username { get; }
        string DisplayName { get; set; }
        public bool IsAuthenticated { get; set; }
        
        string Host { get; set; }
        int Port { get; set; }
        
        string ChannelId { get; set; }
        void SetDisplayName(string displayName);
        void SetUsername(string username);
        void SetAuthenticated();
        string UserServerPort();
        
       
        public Task<string?> ReadAsyncTcp(CancellationToken cts);
        
        public Task<byte[]> ReadAsyncUdp(CancellationToken cts);
        
        Task<bool> WaitConfirmation(byte[] messageBytes, int maxRetransmissions);
        void SendConfirmation(int messageID);
        
        public Task WriteAsync(string message);
        
        Task WriteAsyncUdp(byte[] message, int retranmissions);
        public bool IsConnected();
        
        void Disconnect();
    }
}