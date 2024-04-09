using System.Net;

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
        
        // Methods
        void SetDisplayName(string displayName);
        void SetUsername(string username);
        void SetAuthenticated();
        string UserServerPort();
       
        public Task<string?> ReadAsync();
        public Task WriteAsync(string message);
        public bool IsConnected();
        
        void Disconnect();
    }
}