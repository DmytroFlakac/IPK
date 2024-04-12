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
    
    public int MessageId = -1;
    
    private bool _broadcast = false;
    
    public readonly string BaseRegex = @"^[A-Za-z0-9-]+$";
    public readonly string DisplayRegex = @"^[!-~]{1,20}$";
    public readonly string MSGERRRegex = @"^(MSG|ERR) FROM ([A-Za-z0-9-]+) IS ([\r\n -~]{1,1400})$";
    
    public string MessageRegex =  @"^[\r\n -~]{1,1400}$";
    
    public readonly string JoinRegex = @"^JOIN\s+(\S+)(?:\s+AS\s+(.+))?$";
    public byte[]? Confirm = null;

    // private CancellationTokenSource _cts;
    
    public enum MessageType : int
    {
        CONFIRM,
        REPLY,
        AUTH,
        JOIN,
        MSG,
        ERR,
        BYE
    }
    protected User()
    {
        Username = "Unknown";
        DisplayName = "Unknown";
        IsAuthenticated = false;
        Host = "0.0.0.0";
        Port = 0;
        ChannelId = "default";
    }
    
    public virtual MessageType GetMessageType(string message)
    {
        throw new NotImplementedException("GetMessageType not implemented");
    }
    public void SetConfirmation(byte[] confirm)
    {
        Confirm = confirm;
    }
    
    public virtual MessageType GetMessageType(byte[] message)
    {
        throw new NotImplementedException("GetMessageType not implemented");
    }
    
    // public void CancelUdpListen() => _cts.Cancel();
    //
    // public void NewCts() => _cts = new CancellationTokenSource();
    //
    // public CancellationTokenSource Cts() => _cts;
    
    public void SetDisplayName(string displayName) => DisplayName = displayName;
    public void SetUsername(string username) => Username = username;
    public void SetAuthenticated() => IsAuthenticated = true;
    
    public void BroadcastMessage() => _broadcast = true;
    
    public void EndBroadcast() => _broadcast = false;
    
    public bool IsBroadcasting() => _broadcast;
    public string UserServerPort() => $"{Host}:{Port}";

    public virtual Task<string?> ReadAsyncTcp(CancellationToken cts)
    {
        throw new NotImplementedException("ReadAsyncTcp not implemented");
    }
   
    public virtual Task<byte[]> ReadAsyncUdp()
    {
        throw new NotImplementedException("ReadAsyncUdp not implemented");
    }
    

    public virtual Task WriteAsync(string message)
    {
        throw new NotImplementedException("WriteAsync not implemented");
    }
    
    public virtual Task WriteAsyncUdp(byte[] message, int retranmissions)
    {
        throw new NotImplementedException("WriteAsyncUdp not implemented");
    }
    
    public virtual Task<bool> WaitConfirmation(byte[] messageBytes, int maxRetransmissions)
    {
        throw new NotImplementedException("WaitConfirmation not implemented");
    }

    public virtual bool IsConnected()
    {
        throw new NotImplementedException("IsConnected not implemented");
    }
    
    public virtual void SendConfirmation(int messageID)
    {
        throw new NotImplementedException("SendConfirmation not implemented");
    }
    
    public virtual void SendReply(string message, int messageID, int refId, bool success)
    {
        throw new NotImplementedException("SendReply not implemented");
    }

    public virtual void Disconnect()
    {
        throw new NotImplementedException("Disconnect not implemented");
    }
}