using System.Net;
using System.Net.Sockets;

namespace Server
{
    public class TcpServer : AbstractServer
    {
        private readonly TcpListener _server;
        private readonly object _clientsLock = new object();

        public TcpServer(string ipAddress, int port, List<IUser> users) : base(ipAddress, port, users)
        {
            _server = new TcpListener(IPAddress.Parse(ipAddress), port);
        }

        public override async Task Start()
        {
            _server.Start();
            await AcceptClientsAsync(); // Use asynchronous method to accept clients
        }

        private async Task AcceptClientsAsync()
        {
            while (true)
            {
                Console.WriteLine("Waiting for new client...");
                var client = await _server.AcceptTcpClientAsync();
                Console.WriteLine(" New client connected");
                TcpUser user = new TcpUser(client); 
                
                lock (_clientsLock)
                {
                    Users.Add(user);
                }
                _ = HandleClientAsync(user); // Now 'user' is accessible here
            }
        }

        private async Task HandleClientAsync(IUser user)
        {
            
            try
            {
                while (true)
                {
                    string? message = await user.ReadAsync(); // Read line asynchronously
                    if (message == null) // Client has disconnected gracefully
                    {
                        break;
                    }
                    else if (message.Contains("AUTH"))
                    {
                        Console.WriteLine($"RECV {user.UserServerPort()} | AUTH");
                        HandleAuth(user);
                        continue;
                    }
                    else if(message.Contains("MSG FROM"))
                    {
                        Console.WriteLine($"RECV {user.UserServerPort()} | MSG");
                        await BroadcastMessage(message, user); // Broadcast message asynchronously
                    }
                    else
                    {
                        Console.WriteLine($"RECV: {message}");
                        await BroadcastMessage(message, user); // Broadcast message asynchronously
                    }
                }
            }
            catch (IOException) // Catch exceptions when client disconnects unexpectedly
            {
                await BroadcastMessage("MSG FROM Server IS User Left", null); // Broadcast message asynchronously
            }
            finally
            {
                lock (_clientsLock)
                {
                    Users.Remove(user);
                }
                user.Disconnect();
                await BroadcastMessage("MSG FROM Server IS User Left", null); // Broadcast message asynchronously
            }
        }
        
        public void HandleAuth(IUser user)
        {
            user.WriteAsync("REPLY OK IS Authenticated successfully");
            Console.WriteLine($"SENT {user.UserServerPort()} | REPLY");
            user.SetAuthenticated();
            user.WriteAsync($"MSG FROM Server IS {user.DisplayName} has joined default");
            Console.WriteLine($"SENT {user.DisplayName} | MSG");
        }

        private Task BroadcastMessage(string message, IUser? sender)
        {
            lock (_clientsLock)
            {
                foreach (var user in Users)
                {
                    if (user == sender || !message.Contains("MSG FROM")) continue;
                    user.WriteAsync(message);
                }
            }
            return Task.CompletedTask;
        }
    }
}
