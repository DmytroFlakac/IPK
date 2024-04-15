using System;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace Server
{
    public class UdpMessageHelper
    {
        private User _user;
        
        public UdpMessageHelper(User user)
        {
            _user = user;
        }

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
        
        public bool CheckAuthMessage(byte[] data)
        {
            if (data.Length < 6) return false;
            int currentIndex = 3; 
            int usernameEndIndex = Array.IndexOf(data, (byte)0, currentIndex);
            if (usernameEndIndex == -1 || usernameEndIndex == currentIndex) return false;
            string username = Encoding.UTF8.GetString(data, currentIndex, usernameEndIndex - currentIndex);
            if (!Regex.IsMatch(username, _user.BaseRegex)) return false;
            currentIndex = usernameEndIndex + 1;
            int displayNameEndIndex = Array.IndexOf(data, (byte)0, currentIndex);
            if (displayNameEndIndex == -1 || displayNameEndIndex == currentIndex) return false;
            string displayName = Encoding.UTF8.GetString(data, currentIndex, displayNameEndIndex - currentIndex);
            if (!Regex.IsMatch(displayName, _user.DisplayRegex)) return false;
            currentIndex = displayNameEndIndex + 1;
            int secretEndIndex = Array.IndexOf(data, (byte)0, currentIndex);
            if (secretEndIndex == -1 || secretEndIndex == currentIndex) return false;
            string secret = Encoding.UTF8.GetString(data, currentIndex, secretEndIndex - currentIndex);
            if (secret.Length > 120 || !Regex.IsMatch(secret, _user.BaseRegex)) return false;
            _user.SetUsername(username);
            _user.SetDisplayName(displayName);
            return secretEndIndex == data.Length - 1;
        }
        
        public bool CheckMessage(byte[] data)
        {
            string hex = BitConverter.ToString(data);
            if (data.Length < 3) return false;
            int currentIndex = 3; 
            int displayNameEndIndex = Array.IndexOf(data, (byte)0, currentIndex);
            if (displayNameEndIndex == -1 || displayNameEndIndex == currentIndex) return false;
            string displayName = Encoding.UTF8.GetString(data, currentIndex, displayNameEndIndex - currentIndex);
            if (!Regex.IsMatch(displayName, _user.DisplayRegex)) return false;
            currentIndex = displayNameEndIndex + 1;
            int messageEndIndex = Array.IndexOf(data, (byte)0, currentIndex);
            if (messageEndIndex == -1 || messageEndIndex == currentIndex) return false;
            string message = Encoding.UTF8.GetString(data, currentIndex, messageEndIndex - currentIndex);
            if (message.Length > 1400 || !Regex.IsMatch(message, _user.MessageRegex)) return false;
            if(messageEndIndex == data.Length - 1)
            {
                _user.SetDisplayName(displayName);
                return true;
            }
            return false;
        }
        
        public bool CheckJoin(byte[] data)
        {
            if (data.Length < 3) return false;
            int currentIndex = 3; 
            int channelIdEndIndex = Array.IndexOf(data, (byte)0, currentIndex);
            if (channelIdEndIndex == -1 || channelIdEndIndex == currentIndex) return false;
            string channelId = Encoding.UTF8.GetString(data, currentIndex, channelIdEndIndex - currentIndex);
            if (!Regex.IsMatch(channelId, _user.BaseRegex)) return false;
            currentIndex = channelIdEndIndex + 1;
            int displayNameEndIndex = Array.IndexOf(data, (byte)0, currentIndex);
            if (displayNameEndIndex == -1 || displayNameEndIndex == currentIndex) return false;
            string displayName = Encoding.UTF8.GetString(data, currentIndex, displayNameEndIndex - currentIndex);
            if (!Regex.IsMatch(displayName, _user.DisplayRegex)) return false;
            if (displayNameEndIndex == data.Length - 1)
            {
                _user.SetDisplayName(displayName);
                return true;
            }
            return false;
        }
        
        public string GetJoinChannel(byte[] data)
        {
            if (data.Length < 3) throw new ArgumentException("Message too short to contain a MessageID.");
            int currentIndex = 3; 
            int channelIdEndIndex = Array.IndexOf(data, (byte)0, currentIndex);
            if (channelIdEndIndex == -1 || channelIdEndIndex == currentIndex) throw new ArgumentException("Message format is incorrect.");
            string channelId = Encoding.UTF8.GetString(data, currentIndex, channelIdEndIndex - currentIndex);
            return channelId;
        }
        
        public string GetJoinDisplayName(byte[] data)
        {
            if (data.Length < 3) throw new ArgumentException("Message too short to contain a MessageID.");
            int currentIndex = 3; 
            int channelIdEndIndex = Array.IndexOf(data, (byte)0, currentIndex);
            if (channelIdEndIndex == -1 || channelIdEndIndex == currentIndex) throw new ArgumentException("Message format is incorrect.");
            currentIndex = channelIdEndIndex + 1;
            int displayNameEndIndex = Array.IndexOf(data, (byte)0, currentIndex);
            if (displayNameEndIndex == -1 || displayNameEndIndex == currentIndex) throw new ArgumentException("Message format is incorrect.");
            string displayName = Encoding.UTF8.GetString(data, currentIndex, displayNameEndIndex - currentIndex);
            return displayName;
        }
        
        
        public string BuildStringMessage(byte[] data)
        {
            if (data.Length < 3) throw new ArgumentException("Message too short to contain a MessageID.");
            int currentIndex = 3; 
            int displayNameEndIndex = Array.IndexOf(data, (byte)0, currentIndex);
            if (displayNameEndIndex == -1 || displayNameEndIndex == currentIndex) throw new ArgumentException("Message format is incorrect.");
            string displayName = Encoding.UTF8.GetString(data, currentIndex, displayNameEndIndex - currentIndex);
            currentIndex = displayNameEndIndex + 1;
            int messageEndIndex = Array.IndexOf(data, (byte)0, currentIndex);
            if (messageEndIndex == -1 || messageEndIndex == currentIndex) throw new ArgumentException("Message format is incorrect.");
            string message = Encoding.UTF8.GetString(data, currentIndex, messageEndIndex - currentIndex);
            return $"MSG FROM {displayName} IS {message}";
        }
        
        public static int GetMessageID(byte[] message)
        {
            if (message.Length < 3) throw new ArgumentException("Message too short to contain a MessageID.");
            int messageId = (message[1] << 8) | message[2];
            return messageId;
        }
        
        public static byte[] BuildMessage(string str, int messageID)
        {
            string prefix = "MSG FROM ";
            int prefixEndIndex = str.IndexOf(" IS ");
            if (prefixEndIndex == -1)
                throw new ArgumentException("The string format is incorrect.");
            string displayName = str.Substring(prefix.Length, prefixEndIndex - prefix.Length);
            string messageContents = str.Substring(prefixEndIndex + 4); // Skipping " IS "
            byte[] displayNameBytes = Encoding.UTF8.GetBytes(displayName + "\0");
            byte[] messageContentsBytes = Encoding.UTF8.GetBytes(messageContents + "\0");
            byte[] messageTypeBytes = { (byte)MessageType.MSG };
            byte[] messageIDBytes = BitConverter.GetBytes((ushort)messageID);
            Array.Reverse(messageIDBytes);
            byte[] result = new byte[1 + 2 + displayNameBytes.Length + messageContentsBytes.Length];
            result[0] = messageTypeBytes[0];
            Buffer.BlockCopy(messageIDBytes, 0, result, 1, messageIDBytes.Length);
            Buffer.BlockCopy(displayNameBytes, 0, result, 3, displayNameBytes.Length);
            Buffer.BlockCopy(messageContentsBytes, 0, result, 3 + displayNameBytes.Length, messageContentsBytes.Length);
            return result;
        }
        
        public byte[] BuildReply(string str, int messageID, int refMessageID, bool success)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(str);
            byte[] messageTypeBytes = { (byte)MessageType.REPLY };
            byte[] messageIDBytes = BitConverter.GetBytes((ushort)messageID);
            Array.Reverse(messageIDBytes);
            byte[] refMessageIDBytes = BitConverter.GetBytes((ushort)refMessageID);
            Array.Reverse(refMessageIDBytes);
            byte[] successBytes = { (byte)(success ? 0x01 : 0x00) };
            byte[] result = new byte[1 + 2 + 1 + 2 + messageBytes.Length + 1]; 
            result[0] = messageTypeBytes[0];
            Buffer.BlockCopy(messageIDBytes, 0, result, 1, messageIDBytes.Length);
            result[3] = successBytes[0]; 
            Buffer.BlockCopy(refMessageIDBytes, 0, result, 4, refMessageIDBytes.Length);
            Buffer.BlockCopy(messageBytes, 0, result, 6, messageBytes.Length);
            result[result.Length - 1] = 0x00;
            return result;
        }
        
        public byte[] BuildError(string str, int messageID)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(str);
            byte[] messageTypeBytes = { (byte)MessageType.ERR };
            byte[] messageIDBytes = BitConverter.GetBytes((ushort)messageID);
            Array.Reverse(messageIDBytes);
            byte[] result = new byte[1 + 2 + messageBytes.Length + 1]; 
            result[0] = messageTypeBytes[0];
            Buffer.BlockCopy(messageIDBytes, 0, result, 1, messageIDBytes.Length);
            Buffer.BlockCopy(messageBytes, 0, result, 3, messageBytes.Length);
            result[result.Length - 1] = 0x00;
            return result;
        }
        
        public static byte[] BuildBye(int messageID)
        {
            byte[] messageTypeBytes = { (byte)MessageType.BYE };
            byte[] messageIDBytes = BitConverter.GetBytes((UInt16)messageID);
            byte[] result = new byte[1 + 2];
            result[0] = messageTypeBytes[0];
            Buffer.BlockCopy(messageIDBytes, 0, result, 2, 1);
            return result;
        }
        
        public static byte[] BuildConfirm(int messageID)
        {
            byte[] messageTypeBytes = { (byte)MessageType.CONFIRM };
            byte[] messageIDBytes = BitConverter.GetBytes((UInt16)messageID);
        
            byte[] result = new byte[1 + 2];
            result[0] = messageTypeBytes[0];
            Buffer.BlockCopy(messageIDBytes, 0, result, 2, 1);
        
            return result;
        }
        public static MessageType GetMessageType(byte[] message)
        {
            return (MessageType)message[0];
        }
        
    }
}