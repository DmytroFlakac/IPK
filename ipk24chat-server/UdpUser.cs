using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Net;
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
        IPAddress serverIpAddress = (((IPEndPoint)client.Client.LocalEndPoint!)!).Address;
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
    
    public override async Task<byte[]> ReadAsyncUdp()
    {
        var buffer = await _udpClient.ReceiveAsync();
        return  buffer.Buffer;
    }
    
    public override async Task WriteAsync(string message)
    {
        ++MessageId;
        byte[] buffer = UdpMessageHelper.BuildMessage(message, MessageId);
        Console.WriteLine($"SENT {Host}:{Port} | MSG {BitConverter.ToString(buffer)}");
        await _udpClient.SendAsync(buffer, buffer.Length, _endPoint);
        // string hex = BitConverter.ToString(buffer);
        // Console.WriteLine($"SENT {Host}:{Port} | {hex}");
        await WaitConfirmation(buffer, _maxRetransmissions);
        // Console.WriteLine("CONFIRMED");
    }
    
    public override async Task WriteAsyncUdp(byte[] message, int retranmissions)
    {
        Console.WriteLine($"SENT {Host}:{Port} | {UdpMessageHelper.GetMessageType(message)} {BitConverter.ToString(message)}");
        await _udpClient.SendAsync(message, message.Length, _endPoint);
        if (retranmissions > 0)
            await WaitConfirmation(message, retranmissions);
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
                    // int port = _endPoint.Port;
                    // string host = _endPoint.Address.ToString();
                    // Console.WriteLine($"WAITING from {host}:{port} | {i}");
                    // int severPort = (((IPEndPoint)_udpClient.Client.LocalEndPoint!)!).Port;
                    // string serverHost = ((IPEndPoint)_udpClient.Client.LocalEndPoint).Address.ToString();
                    // Console.WriteLine($"WAITING on {serverHost}:{severPort} | {i}");
                    Confirm = _udpClient.Receive(ref _endPoint);
                }
                UdpMessageHelper.MessageType messageType = UdpMessageHelper.GetMessageType(Confirm);
                Console.WriteLine($"RECV {Host}:{Port} | {messageType} {BitConverter.ToString(Confirm)}");
               
                if (UdpMessageHelper.GetMessageType(Confirm) == UdpMessageHelper.MessageType.CONFIRM &&
                    UdpMessageHelper.GetMessageID(Confirm) == MessageId)
                {
                    Confirm = null;
                    return true;
                }
                else
                {
                    await WriteAsyncUdp(messageBytes, 0);
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
        
        _udpClient.Close();
    }
}