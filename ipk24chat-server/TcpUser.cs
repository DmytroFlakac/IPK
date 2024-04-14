using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Server;

public class TcpUser : User
{
    private readonly TcpClient _tcpClient;
    private  NetworkStream _stream;
    private StreamReader _reader;
    
    public TcpUser(TcpClient client) 
    {
        _tcpClient = client;
        _stream = _tcpClient.GetStream();
        _reader = new StreamReader(_stream, Encoding.UTF8, leaveOpen: true);
        Port = ((IPEndPoint)client.Client.RemoteEndPoint!).Port;
        Host = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
    }

    public override async Task<string?> ReadAsyncTcp(CancellationToken cts)
    {
        string? line = await _reader.ReadLineAsync(cts).ConfigureAwait(false);
        return line?.Trim(); 
    }

    
    public override async Task WriteAsync(string message)
    {
        try
        {
            Console.WriteLine($"SENT {Host}:{Port} | {GetMessageType(message)} {message}");
            var buffer = Encoding.UTF8.GetBytes(message + "\r\n");
            await _stream.WriteAsync(buffer, 0, buffer.Length);
        }
        catch (Exception e)
        {
            // Ignore
        }
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
        else if(message.Contains("REPLY"))
            return MessageType.REPLY;
        else
            return MessageType.ERR;
    }

    public override void Disconnect()
    {
        Active = false;
        _stream.Close();
        _tcpClient.Close();
    }
}