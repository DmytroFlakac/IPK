using System;
using System.Net.Sockets;
using System.Text;

namespace IPK24Chat
{
   public enum MessageType : byte
    {
        CONFIRM = 0x00,
        REPLY = 0x01,
        AUTH = 0x02,
        JOIN = 0x03,
        MSG = 0x04,
        ERR = 0xFE,
        BYE = 0xFF
    }

    public class UDPmessageHelper
    {
        public static byte[] buildMessage(string username, string secret, string displayName, int messageID, MessageType messageType = MessageType.AUTH)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes($"{username}\0{displayName}\0{secret}\0");
            byte[] messageTypeBytes = new byte[] { (byte)messageType };
            byte[] messageIDBytes = BitConverter.GetBytes((UInt16)messageID);

            byte[] result = new byte[1 + 2 + messageBytes.Length];
            result[0] = messageTypeBytes[0];
            Buffer.BlockCopy(messageIDBytes, 0, result, 1, 2);
            Buffer.BlockCopy(messageBytes, 0, result, 3, messageBytes.Length);

            return result;
        }

        public static byte[] buildMessage(string channelID, int messageID, MessageType messageType = MessageType.JOIN)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(channelID);
            byte[] messageTypeBytes = new byte[] { (byte)messageType };
            byte[] messageIDBytes = BitConverter.GetBytes((UInt16)messageID);

            byte[] result = new byte[1 + 2 + messageBytes.Length];
            result[0] = messageTypeBytes[0];
            Buffer.BlockCopy(messageIDBytes, 0, result, 1, 2);
            Buffer.BlockCopy(messageBytes, 0, result, 3, messageBytes.Length);

            return result;
        }

        public static byte[] buildMessage(int messageID, MessageType messageType = MessageType.CONFIRM)
        {
            byte[] messageTypeBytes = new byte[] { (byte)messageType };
            byte[] messageIDBytes = BitConverter.GetBytes((UInt16)messageID);

            byte[] result = new byte[1 + 2];
            result[0] = messageTypeBytes[0];
            Buffer.BlockCopy(messageIDBytes, 0, result, 1, 2);

            return result;
        }

        public static byte[] buildMessage(int messageID, string message,string displayName, MessageType messageType = MessageType.MSG)//byte format: type(1B) + ID(2B) + displayname(UTF8) + message(UTF8)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes($"{displayName}\0{message}\0");
            byte[] messageTypeBytes = new byte[] { (byte)messageType };
            byte[] messageIDBytes = BitConverter.GetBytes((UInt16)messageID);

            byte[] result = new byte[1 + 2 + messageBytes.Length];
            result[0] = messageTypeBytes[0];
            Buffer.BlockCopy(messageIDBytes, 0, result, 1, 2);
            Buffer.BlockCopy(messageBytes, 0, result, 3, messageBytes.Length);

            return result;
        }

        public static int getMessageID(byte[] message)
        {
            byte[] messageIDBytes = new byte[2];
            Buffer.BlockCopy(message, 1, messageIDBytes, 0, 2);
            return BitConverter.ToUInt16(messageIDBytes, 0);
        }

        public static int getRefMessageID(byte[] message)
        {
            byte[] messageIDBytes = new byte[2];
            Buffer.BlockCopy(message, 4, messageIDBytes, 0, 2);
            return BitConverter.ToUInt16(messageIDBytes, 0);
        }

        public static int getReplyResult(byte[] message)
        {
            return message[3]; // Implicitly converts byte to int
        }

        public static string getReplyMessageContents(byte[] message)
        {
            int startIndex = 6; 
            int length = message.Length - startIndex - 1; 
            byte[] contentsBytes = new byte[length];
            Buffer.BlockCopy(message, startIndex, contentsBytes, 0, length);
            return Encoding.UTF8.GetString(contentsBytes);
        }

        public static string getReplyDisplayName(byte[] message)
        {
            int startIndex = 3;
            int length = message.Length - startIndex - 1;
            byte[] contentsBytes = new byte[length];
            Buffer.BlockCopy(message, startIndex, contentsBytes, 0, length);
            return Encoding.UTF8.GetString(contentsBytes);
        }

        public static string getMSGdisplayName(byte[] message)
        {
            int startIndex = 3;
            int endIndex = Array.IndexOf(message, (byte)0, startIndex);
            int length = endIndex - startIndex;

            byte[] displayNameBytes = new byte[length];
            Buffer.BlockCopy(message, startIndex, displayNameBytes, 0, length);

            return Encoding.UTF8.GetString(displayNameBytes);
        }

        public static string getMSGContents(byte[] message)
        {
            int startIndex = Array.IndexOf(message, (byte)0, 3) + 1;
            int endIndex = Array.IndexOf(message, (byte)0, startIndex);
            int length = endIndex - startIndex;

            byte[] messageContentsBytes = new byte[length];
            Buffer.BlockCopy(message, startIndex, messageContentsBytes, 0, length);

            return Encoding.UTF8.GetString(messageContentsBytes);
        }
        public static MessageType getMessageType(byte[] message)
        {
            return (MessageType)message[0];
        }

        public static void printMessage(byte[] message)
        {
            Console.WriteLine($"Message type: {getMessageType(message)}");
            Console.WriteLine($"Message ID: {getMessageID(message)}");
        }
        
    }
}