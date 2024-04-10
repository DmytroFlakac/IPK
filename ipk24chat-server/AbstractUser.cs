using System;
using System.Net;
using System.Threading.Tasks;

namespace Server;
public abstract class User : IUser
{
    public string Username { get; set; }
    public string DisplayName { get; set; }
    
    public bool IsAuthenticated { get; set; }
    
    public string Host { get; set; }
    public int Port { get; set; }
    
    public string ChannelId { get; set; }
    
    public string BaseRegex { get; set; }
    public string DisplayRegex { get; set; }
    public string MessageRegex { get; set; }
    
    public string JoinRegex { get; set; }
    

    protected User()
    {
        Username = "Unknown";
        DisplayName = "Unknown";
        IsAuthenticated = false;
        Host = "0.0.0.0";
        Port = 0;
        BaseRegex = @"^[A-Za-z0-9-]+$";
        DisplayRegex = @"^[!-~]{1,20}$";
        MessageRegex = @"^(MSG|ERR) FROM ([A-Za-z0-9-]+) IS ([\r\n -~]{1,1400})$";
        JoinRegex = @"^JOIN\s+(\S+)(?:\s+AS\s+(.+))?$";
        ChannelId = "default";
    }
    
    public void SetDisplayName(string displayName) => DisplayName = displayName;
    public void SetUsername(string username) => Username = username;
    public void SetAuthenticated() => IsAuthenticated = true;
    
    public string UserServerPort() => $"{Host}:{Port}";

    public virtual Task<string?> ReadAsync()
    {
        throw new NotImplementedException("ReadlineAsync not implemented");
    }

    public virtual Task WriteAsync(string message)
    {
        throw new NotImplementedException("WriteAsync not implemented");
    }

    public virtual bool IsConnected()
    {
        throw new NotImplementedException("IsConnected not implemented");
    }
    
    
    // public virtual void SendMessage(string message)
    // {
    //     throw new NotImplementedException("SendMessage not implemented");
    // }
    
    // public virtual void ReceiveMessage(string message)
    // {
    //     throw new NotImplementedException("ReceiveMessage not implemented");
    // }

    public virtual void Disconnect()
    {
        throw new NotImplementedException("Disconnect not implemented");
    }
}