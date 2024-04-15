using System;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Server;

public class UdpUser : User
{
    private readonly UdpClient _udpClient;
    private IPEndPoint _endPoint;
    private int _maxRetransmissions;
    
    
    public enum UdpMessageType : byte
    {
        CONFIRM = 0x00,
        REPLY = 0x01,
        AUTH = 0x02,
        JOIN = 0x03,
        MSG = 0x04,
        ERR = 0xFE,
        BYE = 0xFF
    }
    public UdpUser(UdpClient client, IPEndPoint endPoint, int retransmissionTimeout, int maxRetransmissions)
    {
        int newPort = GetAvailablePort();
        IPAddress serverIpAddress = ((IPEndPoint)client.Client.LocalEndPoint!).Address;
        _udpClient = new UdpClient(new IPEndPoint(serverIpAddress, newPort));
        _endPoint = endPoint;
        Port = endPoint.Port;
        Host = endPoint.Address.ToString();
        _udpClient.Client.ReceiveTimeout = retransmissionTimeout;
        _udpClient.Client.SendTimeout = retransmissionTimeout;
        _maxRetransmissions = maxRetransmissions;
        
    }
    private int GetAvailablePort()
    {
        using (var tempSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
        {
            tempSocket.Bind(new IPEndPoint(IPAddress.Any, 0));
            return ((IPEndPoint)tempSocket.LocalEndPoint!).Port;
        }
    }
    
    public override async Task<byte[]> ReadAsyncUdp(CancellationToken cts)
    {
        var buffer = await _udpClient.ReceiveAsync(cts);
        return  buffer.Buffer;
    }
    
    public override async Task WriteAsync(string message)
    {
        try
        {
            ++MessageId;
            byte[] buffer = UdpMessageHelper.BuildMessage(message, MessageId);
            Console.WriteLine(
                $"SENT {Host}:{Port} | {UdpMessageHelper.GetMessageType(buffer)} {BitConverter.ToString(buffer)}");
            await _udpClient.SendAsync(buffer, buffer.Length, _endPoint);
            await WaitConfirmation(buffer, _maxRetransmissions);
        }
        catch (SocketException)
        {
           //ignore
        }
    }
    
    public override async Task WriteAsyncUdp(byte[] message, int retransmissions)
    {
        Console.WriteLine($"SENT {Host}:{Port} | {UdpMessageHelper.GetMessageType(message)} {BitConverter.ToString(message)}");
        await _udpClient.SendAsync(message, message.Length, _endPoint);
        if (retransmissions > 0)
            await WaitConfirmation(message, retransmissions);
    }
    
    public override void SendConfirmation(int messageID)
    {
        Console.WriteLine($"SENT {Host}:{Port} | CONFIRM {BitConverter.ToString(UdpMessageHelper.BuildConfirm(messageID))}");
        byte[] messageBytes = UdpMessageHelper.BuildConfirm(messageID);
        _udpClient.Send(messageBytes, messageBytes.Length, _endPoint);
    }
    
    public override async Task<bool> WaitConfirmation(byte[] messageBytes, int maxRetransmissions)
    {
        for (int i = 0; i < maxRetransmissions; i++)
        {
            try
            {
                if (Confirm == null)
                {
                    Confirm = _udpClient.Receive(ref _endPoint);
                }
                if (UdpMessageHelper.GetMessageType(Confirm) == UdpMessageHelper.MessageType.CONFIRM &&
                    UdpMessageHelper.GetMessageID(Confirm) == MessageId)
                {
                    Confirm = null;
                    return true;
                }
                else
                {
                    Console.WriteLine($"SENT {Host}:{Port} | {UdpMessageHelper.GetMessageType(messageBytes)} {BitConverter.ToString(messageBytes)}");
                    await _udpClient.SendAsync(messageBytes, messageBytes.Length, _endPoint);
                    Confirm = null;
                }
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode == SocketError.TimedOut)
                {
                    continue;
                }
            }
        }
        return false;
    }
    
    public override bool IsConnected() => true;

    public override MessageType GetMessageType(byte[] message)
    {
        if (message[0] == (byte)UdpMessageType.AUTH)
            return MessageType.AUTH;
        else if(message[0] == (byte)UdpMessageType.JOIN)
            return MessageType.JOIN;
        else if(message[0] == (byte)UdpMessageType.MSG)
            return MessageType.MSG;
        else if(message[0] == (byte)UdpMessageType.ERR)
            return MessageType.ERR;
        else if (message[0] == (byte)UdpMessageType.BYE)
            return MessageType.BYE;
        else if (message[0] == (byte)UdpMessageType.CONFIRM)
            return MessageType.CONFIRM;
        else
            return MessageType.ERR;
    }

    public override void Disconnect()
    {
        Active = false;
        _udpClient.Close();
    }
}