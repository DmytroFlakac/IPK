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
    
    public override async Task<string?> ReadAsync() => await new StreamReader(_stream).ReadLineAsync();
    
    public override async Task WriteAsync(string message)
    {
        var buffer = Encoding.UTF8.GetBytes(message);
        await _stream.WriteAsync(buffer, 0, buffer.Length);
    }
    
    public override bool IsConnected() => _tcpClient.Connected;
    

    public override void Disconnect()
    {
        _stream.Close();
        _tcpClient.Close();
    }
}