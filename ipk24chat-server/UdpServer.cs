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
        public UdpMessageHelper UdpMessageHelper;
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

        public override async Task Start(CancellationToken cts)
        {
            var tasks = new List<Task>();
            try
            {
                while (!cts.IsCancellationRequested)
                {
                    var result = await _server.ReceiveAsync(cts);
                    byte[] message = result.Buffer;
                    var user = new UdpUser(_server, result.RemoteEndPoint, _server.Client.ReceiveTimeout, _maxRetransmissions); 
                    UdpMessageHelper = new UdpMessageHelper(user);

                    lock (ClientsLock)
                    {
                        if (!Channels.ContainsKey(ChannelId))
                        {
                            Channels.Add(ChannelId, new List<User>());
                        }
                        Channels[ChannelId].Add(user);
                    }
                    var clientTask = HandleClientAsync(user, cts); // Store the task
                    tasks.Add(clientTask);
                }
            }
            catch (OperationCanceledException)
            {
                // Ignore
            }
            finally
            {
                await Task.WhenAll(tasks); // Ensure all tasks complete or are cancelled
            }
        }
        
        
        
        
        public override async Task HandleClientAsync(User user, byte[] message, CancellationToken cts)
        {
            try
            {
                while (!cts.IsCancellationRequested)
                {
                   
                    if (message == null) 
                    {
                        continue;   
                    }
                    User.MessageType messageType = user.GetMessageType(message);
                    Console.WriteLine($"RECV {user.UserServerPort()} | {messageType} {BitConverter.ToString(message)}");
                    
                    switch (messageType)
                    {
                        case User.MessageType.AUTH:
                            HandleAuth(user, message);
                            break;
                        case User.MessageType.JOIN:
                            HandleJoin(user, message);
                            break;
                        case User.MessageType.MSG:
                            HandleMessage(user, message);
                            break;
                        case User.MessageType.CONFIRM:
                            user.SetConfirmation(message);
                            break;
                        case User.MessageType.ERR:
                            // HandleERR_FROM(user, message);
                            break;
                        case User.MessageType.BYE:
                            HandleBye(user);
                            break;
                        default:
                            byte[] err = UdpMessageHelper.BuildError("Invalid message format", user.MessageId);
                            await user.WriteAsyncUdp(err, _maxRetransmissions);
                            HandleBye(user);
                            break;
                    }
                    
                    message = await user.ReadAsyncUdp();
                    
                }
            }
            catch (IOException) // Catch exceptions when client disconnects unexpectedly
            {
                byte[] bye = UdpMessageHelper.BuildBye(user.MessageId);
                Console.WriteLine($"RECV {user.UserServerPort()} | BYE {BitConverter.ToString(bye)}");
                await user.WriteAsyncUdp(bye, _maxRetransmissions);
                user.Disconnect();
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"SENT {user.UserServerPort()} | BYE BYE");
                await user.WriteAsync("BYE");
                user.Disconnect();
                
            }
        }
        
        public override async void HandleAuth(User user, byte[] message)
        {
            int refMessageId = UdpMessageHelper.GetMessageID(message);
            user.SendConfirmation(refMessageId);
            if (UdpMessageHelper.CheckAuthMessage(message))
            {
                user.SetAuthenticated();
                user.MessageId = user.MessageId + 1;
                byte[] reply = UdpMessageHelper.BuildReply("Authenticated", user.MessageId, refMessageId, true);
                await user.WriteAsyncUdp(reply, 0);
                if(!(await user.WaitConfirmation(reply, _maxRetransmissions)))
                {
                    byte[] err = UdpMessageHelper.BuildError("Failed to authenticate", user.MessageId);
                    await user.WriteAsyncUdp(err, _maxRetransmissions);
                    return;
                }
                await BroadcastMessage($"MSG FROM Server IS {user.DisplayName} has joined default", null);
            }
            else
            {
                user.MessageId = user.MessageId + 1;
                byte[] reply = UdpMessageHelper.BuildReply("Failed to authenticate", user.MessageId, refMessageId, false);
                await user.WriteAsyncUdp(reply, 0);
            }
        }
        
        public override async void HandleJoin(User user, byte[] message)
        {
            user.SendConfirmation(UdpMessageHelper.GetMessageID(message));
            if (!UdpMessageHelper.CheckJoin(message))
            {
                user.MessageId = user.MessageId + 1;
                byte[] reply = UdpMessageHelper.BuildReply("Failed to join", user.MessageId,
                    UdpMessageHelper.GetMessageID(message), false);
                await user.WriteAsyncUdp(reply, _maxRetransmissions);
                return;
            }
            user.SetDisplayName(UdpMessageHelper.GetJoinDisplayName(message));
            await BroadcastMessage($"MSG FROM Server IS {user.DisplayName} has left {user.ChannelId}", user, user.ChannelId);
            string channelId = UdpMessageHelper.GetJoinChannel(message);
            AddUser(user, channelId);
            await BroadcastMessage($"MSG FROM Server IS {user.DisplayName} has joined {channelId}", null, channelId);
            user.MessageId = user.MessageId + 1;
            byte[] replyJoin = UdpMessageHelper.BuildReply($"Joined {channelId}", user.MessageId, UdpMessageHelper.GetMessageID(message), true);
            await user.WriteAsyncUdp(replyJoin, _maxRetransmissions);
        }
        public override async void HandleMessage(User user, byte[] message)
        {
            user.SendConfirmation(UdpMessageHelper.GetMessageID(message));
            if (UdpMessageHelper.CheckMessage(message))
            {
                string msg = UdpMessageHelper.BuildStringMessage(message);
                await BroadcastMessage(msg, user, user.ChannelId);
            }
            else
            {
                byte[] err = UdpMessageHelper.BuildError("Invalid message format", user.MessageId);
                await user.WriteAsyncUdp(err, _maxRetransmissions);
            }
        }
        
        
        public override async void HandleBye(User user, byte[] message)
        {
            user.SendConfirmation(UdpMessageHelper.GetMessageID(message));
            user.Disconnect();
            Console.WriteLine($"RECV {user.UserServerPort()} | BYE {BitConverter.ToString(message)}");
            lock (ClientsLock)
            {
                Channels[user.ChannelId].Remove(user);
            }
            await BroadcastMessage($"MSG FROM Server IS {user.DisplayName} has left {user.ChannelId}", user, user.ChannelId);
        }
        
        
    }
}