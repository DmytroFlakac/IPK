using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    public class UdpServer : AbstractServer
    {
        private readonly UdpClient _server;
        private readonly int _maxRetransmissions;
        private UdpMessageHelper _udpMessageHelper;
        private int _messageId = -1;

        public UdpServer(string ipAddress, int port, Dictionary<string, List<User>> channels, int retransmissionTimeout, int maxRetransmissions, string channelId = "default") :
            base(ipAddress, port, channels, channelId)
        {
            var endPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
            _server = new UdpClient(endPoint);
            _server.Client.ReceiveTimeout = retransmissionTimeout;
            _server.Client.SendTimeout = retransmissionTimeout;
            _maxRetransmissions = maxRetransmissions;
        }
        
        
        public override async Task Start()
        {
            await AcceptClientsAsync(); 
        }

        public override async Task AcceptClientsAsync()
        {
            while (true)
            {
                var result = await _server.ReceiveAsync();
                byte[] message = result.Buffer;
                
                
                var client = new UdpUser(_server, result.RemoteEndPoint); 
                _udpMessageHelper = new UdpMessageHelper(client);
                lock (ClientsLock)
                {
                    if (!Channels.ContainsKey(ChannelId))
                    {
                        Channels.Add(ChannelId, new List<User>());
                    }
                    Channels[ChannelId].Add(client);
                }
                _ = HandleClientAsync(client, message);
            }
        }
        
        
        
        
        public override async Task HandleClientAsync(User user, byte[] message)
        {
             CancellationTokenSource cts = new CancellationTokenSource();
            try
            {
                
                while (!cts.Token.IsCancellationRequested)
                {
                   
                    if (message == null) 
                    {
                        continue;   
                    }
                    User.MessageType messageType = user.GetMessageType(message);
                    string hex = BitConverter.ToString(message);
                    
                    
                    switch (messageType)
                    {
                        case User.MessageType.AUTH:
                            HandleAuth(user, message);
                            break;
                        case User.MessageType.JOIN:
                            // HandleJoin(user, message);
                            break;
                        case User.MessageType.MSG:
                            HandleMessage(user, message);
                            break;
                        case User.MessageType.CONFIRM:
                            Console.WriteLine($"RECV {user.UserServerPort()} | CONFIRM {hex}");
                            user.SetConfirmation(message);
                            break;
                        case User.MessageType.ERR:
                            Console.WriteLine($"RECV {user.UserServerPort()} | {messageType} {hex}");
                            // HandleERR_FROM(user, message);
                            break;
                        case User.MessageType.BYE:
                            // HandleBye(user);
                            break;
                        default:
                            Console.WriteLine($"SENT {user.UserServerPort()} | ERR Invalid message format");
                            await user.WriteAsync("ERR FROM Server IS Invalid message format\r\n");
                            // HandleBye(user);
                            break;
                    }
                    
                    message = await user.ReadAsyncUdp();
                    
                }
            }
            catch (IOException) // Catch exceptions when client disconnects unexpectedly
            {
                // HandleBye(user);  
                cts.Cancel();
            }
            finally
            {
                // HandleBye(user);
                cts.Cancel();
            }
        }
        
        public override async void HandleAuth(User user, byte[] message)
        {
            Console.WriteLine($"RECV {user.UserServerPort()} | AUTH {message}");
            int refMessageId = UdpMessageHelper.GetMessageID(message);
            Console.WriteLine($"SENT {user.UserServerPort()} | CONFIRM {refMessageId}");
            user.SendConfirmation(refMessageId);
            if (_udpMessageHelper.CheckAuthMessage(message))
            {
                user.SetAuthenticated();
                Console.WriteLine($"SENT {user.UserServerPort()} | REPLY {refMessageId}");
                user.MessageId = user.MessageId + 1;
                byte[] reply = _udpMessageHelper.BuildReply("Authenticated", user.MessageId, refMessageId, true);
                string hex = BitConverter.ToString(reply);
                Console.WriteLine($"SENT {user.UserServerPort()} | REPLY {hex}");
                await user.WriteAsyncUdp(reply, 0);
                if(!(await user.WaitConfirmation(reply, _maxRetransmissions)))
                {
                    Console.WriteLine($"SENT {user.UserServerPort()} | ERR Failed to authenticate");
                    // byte[] err = _udpMessageHelper.BuildError("Failed to authenticate", user.MessageId);
                    // user.WriteAsyncUdp(err, 0);
                    return;
                }
                Console.WriteLine($"SENT {user.UserServerPort()} | MSG FROM Server IS {user.DisplayName} has joined default");
                await BroadcastMessage($"MSG FROM Server IS {user.DisplayName} has joined default", null);
            }
            else
            {
                Console.WriteLine($"SENT {user.UserServerPort()} | REPLY {refMessageId}");
                user.SendReply("Failed to authenticate", _messageId, refMessageId, false);
            }
        }
        
        
        public override async void HandleMessage(User user, byte[] message)
        {
            if (_udpMessageHelper.CheckMessage(message))
            {
                Console.WriteLine($"SENT {user.UserServerPort()} | CONFIRM {UdpMessageHelper.GetMessageID(message)}");
                user.SendConfirmation(UdpMessageHelper.GetMessageID(message));
                string msg = _udpMessageHelper.BuildStringMessage(message);
                Console.WriteLine($"SENT {user.UserServerPort()} | MSG {msg}");
                await BroadcastMessage(msg, user);
            }
            else
            {
                Console.WriteLine("SENT ERR Invalid message format");
            }
        }
        
        
    }
}