using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    public class UdpServer : AbstractServer
    {
        private readonly UdpClient _server;
        private readonly int _maxRetransmissions;
        public UdpMessageHelper UdpMessageHelper;

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
                    // Console.WriteLine("ReceiveAsync before");
                    var result = await _server.ReceiveAsync(cts);
                    // Console.WriteLine("ReceiveAsync after");
                    byte[] message = result.Buffer;
                    var user = new UdpUser(_server, result.RemoteEndPoint, _server.Client.ReceiveTimeout, _maxRetransmissions); 

                    lock (ClientsLock)
                    {
                        if (!Channels.ContainsKey(ChannelId))
                        {
                            Channels.Add(ChannelId, new List<User>());
                        }
                        AddUser(user, "Unknown");
                    }
                    // Console.WriteLine("HandleClientAsync before");
                    var clientTask = HandleClientAsync(user, message, cts);
                    // Console.WriteLine("HandleClientAsync after");
                    tasks.Add(clientTask);
                }
            }
            catch (OperationCanceledException)
            {
                // Ignore
            }
            finally
            {
                await Task.WhenAll(tasks);
            }
        }
        
        public override async Task HandleClientAsync(User user, byte[] message, CancellationToken cts)
        {   
            CancellationTokenSource cts2 = new CancellationTokenSource();
            UdpMessageHelper = new UdpMessageHelper(user);
            try
            {
                int messageID = UdpMessageHelper.GetMessageID(message);
                int currentMessageID = UdpMessageHelper.GetMessageID(message);
                while (!cts2.IsCancellationRequested)
                {
                    cts.Register(() => cts2.Cancel());
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
                            HandleERR_FROM(user, message);
                            break;
                        case User.MessageType.BYE:
                            HandleBye(user, message);
                            break;
                        default:
                            byte[] err = UdpMessageHelper.BuildError("Invalid message format", user.MessageId);
                            await user.WriteAsyncUdp(err, _maxRetransmissions);
                            CleanUser(user);
                            break;
                    }
                    if(!user.Active)
                        cts2.Cancel();
                    while(messageID == currentMessageID && !cts2.IsCancellationRequested)
                    {
                        if (!cts2.IsCancellationRequested)
                            message = await user.ReadAsyncUdp(cts2.Token);
                        currentMessageID = UdpMessageHelper.GetMessageID(message);
                    }
                    messageID = currentMessageID;
                }
            }
            catch (IOException) 
            {
                cts2.Cancel();
                CleanUser(user);
            }
            catch (OperationCanceledException)
            {
                cts2.Cancel();
                CleanUser(user);
            }
        }
        
        public override async void HandleAuth(User user, byte[] message)
        {
            int refMessageId = UdpMessageHelper.GetMessageID(message);
            user.SendConfirmation(refMessageId);
            if (UdpMessageHelper.CheckAuthMessage(message) && !ExistedUser(user))
            {
                user.SetAuthenticated();
                user.MessageId = user.MessageId + 1;
                byte[] reply = UdpMessageHelper.BuildReply("Authenticated", user.MessageId, refMessageId, true);
                await user.WriteAsyncUdp(reply, 0);
                if(!await user.WaitConfirmation(reply, _maxRetransmissions))
                    return;
                user.SetAuthenticated();
                user.MessageId = user.MessageId + 1;
                AddUser(user, "default");
                byte[] msg = UdpMessageHelper.BuildMessage($"MSG FROM Server IS {user.DisplayName} has joined default", user.MessageId);
                await user.WriteAsyncUdp(msg, _maxRetransmissions);
                var broadcast = BroadcastMessage($"MSG FROM Server IS {user.DisplayName} has joined default", user);
            }
            else
            {
                user.MessageId = user.MessageId + 1;
                byte[] reply = UdpMessageHelper.BuildReply("Failed to authenticate", user.MessageId, refMessageId, false);
                await user.WriteAsyncUdp(reply, _maxRetransmissions);
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
            var broadcast = BroadcastMessage($"MSG FROM Server IS {user.DisplayName} has left {user.ChannelId}", user, user.ChannelId);
            string channelId = UdpMessageHelper.GetJoinChannel(message);
            
            user.MessageId = user.MessageId + 1;
            byte[] msg = UdpMessageHelper.BuildMessage($"MSG FROM Server IS {user.DisplayName} has joined {channelId}", user.MessageId);
            await user.WriteAsyncUdp(msg, _maxRetransmissions);
            user.MessageId = user.MessageId + 1;
            byte[] replyJoin = UdpMessageHelper.BuildReply($"Joined {channelId}", user.MessageId, UdpMessageHelper.GetMessageID(message), true);
            await user.WriteAsyncUdp(replyJoin, _maxRetransmissions);
            AddUser(user, channelId);
            var broadcast1 = BroadcastMessage($"MSG FROM Server IS {user.DisplayName} has joined {channelId}", user, channelId);
        }
        public override async void HandleMessage(User user, byte[] message)
        {
            user.SendConfirmation(UdpMessageHelper.GetMessageID(message));
            if (UdpMessageHelper.CheckMessage(message))
            {
                string msg = UdpMessageHelper.BuildStringMessage(message);
                var broadcast = BroadcastMessage(msg, user, user.ChannelId);
            }
            else
            {
                user.MessageId = user.MessageId + 1;
                byte[] err = UdpMessageHelper.BuildError("Invalid message format", user.MessageId);
                await user.WriteAsyncUdp(err, _maxRetransmissions);
                CleanUser(user);
            }
        }

        public override async void HandleERR_FROM(User user, byte[] message)
        {
            user.SendConfirmation(UdpMessageHelper.GetMessageID(message));
            if (UdpMessageHelper.CheckMessage(message))
            {
                CleanUser(user);
            }
            else
            {
                user.MessageId = user.MessageId + 1;
                byte[] err = UdpMessageHelper.BuildError("Invalid message format", user.MessageId);
                await user.WriteAsyncUdp(err, _maxRetransmissions);
                CleanUser(user);
            }

        }
        
        public override void HandleBye(User user, byte[] message)
        {
            user.SendConfirmation(UdpMessageHelper.GetMessageID(message));
            lock (ClientsLock) Channels[user.ChannelId].Remove(user);
            user.Disconnect();
            var broadcast = BroadcastMessage($"MSG FROM Server IS {user.DisplayName} has left {user.ChannelId}", user, user.ChannelId);
        }

        public override async void CleanUser(User user)
        {
            try
            {
                user.IsAuthenticated = false;
                user.MessageId = user.MessageId + 1;
                await user.WriteAsyncUdp(UdpMessageHelper.BuildBye(user.MessageId), _maxRetransmissions);
                var broadcast = BroadcastMessage($"MSG FROM Server IS {user.DisplayName} has left {user.ChannelId}", user,
                    user.ChannelId);
                user.Disconnect();
                lock (ClientsLock) Channels[user.ChannelId].Remove(user);
            }
            catch (Exception)
            {
                // Ignore
            }
        }
    }
}