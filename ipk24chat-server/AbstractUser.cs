using System;
using System.Net;

namespace Server;
public abstract class User : IUser
{
    public string Username { get; set; }
    public string DisplayName { get; set; }
    
    public bool IsAuthenticated { get; set; }
    
    public string Host { get; set; }
    public int Port { get; set; }
    

    protected User()
    {
        Username = "Unknown";
        DisplayName = "Unknown";
    }
    
    public void SetDisplayName(string displayName) => DisplayName = displayName;
    public void SetUsername(string username) => Username = username;
    public void SetAuthenticated() => IsAuthenticated = true;
    
    public virtual string UserServerPort()
    {
        throw new NotImplementedException("UserServerPort not implemented");
    }

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