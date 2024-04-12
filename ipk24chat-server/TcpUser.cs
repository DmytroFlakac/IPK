using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Net;
using System.Threading.Tasks;

namespace Server;

public class TcpUser : User
{
    private readonly TcpClient _tcpClient;
    private readonly NetworkStream _stream;
    
    public TcpUser(TcpClient client) 
    {
        _tcpClient = client;
        _stream = _tcpClient.GetStream();
        Port = ((IPEndPoint)client.Client.RemoteEndPoint).Port;
        Host = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
    }
    
    public override async Task<string?> ReadAsyncTcp() => await new StreamReader(_stream).ReadLineAsync();
    
    public override async Task WriteAsync(string message)
    {
        var buffer = Encoding.UTF8.GetBytes(message + "\r\n");
        await _stream.WriteAsync(buffer, 0, buffer.Length);
    }
    
    public override bool IsConnected() => _tcpClient.Connected;

    public override MessageType GetMessageType(string message)
    {
        if (message.Contains("AUTH"))
            return MessageType.AUTH;
        else if(message.Contains("JOIN"))
            return MessageType.JOIN;
        else if(message.Contains("MSG FROM"))
            return MessageType.MSG;
        else if(message.Contains("ERR FROM"))
            return MessageType.ERR;
        else if (message == "BYE")
            return MessageType.BYE;
        else
            return MessageType.ERR;
    }

    public override void Disconnect()
    {
        _stream.Close();
        _tcpClient.Close();
    }
}