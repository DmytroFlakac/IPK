using System.Net;
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
        
        // Regex

        // Methods
        void SetDisplayName(string displayName);
        void SetUsername(string username);
        void SetAuthenticated();
        string UserServerPort();
       
        public Task<string?> ReadAsyncTcp();
        
        public Task<byte[]> ReadAsyncUdp();
        
        public Task WriteAsync(string message);
        public bool IsConnected();
        
        void Disconnect();
    }
}