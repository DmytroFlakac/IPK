using System.Net;
using System.Net.Sockets;


namespace Server;
public class TcpServer
{
    private TcpListener _server;
    private List<TcpClient> _clients = new List<TcpClient>();
    private readonly object _clientsLock = new object();

    public TcpServer(string ipAddress, int port)
    {
        _server = new TcpListener(IPAddress.Parse(ipAddress), port);
    }

    public void Start()
    {
        _server.Start();
        var serverInputThread = new Thread(HandleServerInput); // Create a new thread for server input
        serverInputThread.Start(); // Start the server input thread
        AcceptClients();
    }

    private void AcceptClients()
    {
        while (true)
        {
            var client = _server.AcceptTcpClient();
            lock (_clientsLock)
            {
                _clients.Add(client);
            }
            var thread = new Thread(HandleClient);
            thread.Start(client);
        }
    }


    private void HandleClient(object obj)
    {
        var client = (TcpClient)obj;
        var stream = client.GetStream();
        var reader = new StreamReader(stream);
    
        try
        {
            while (true)
            {
                var message = reader.ReadLine();
                if (message == null) // Client has disconnected gracefully
                {
                    break;
                }
                else if(message.Contains("AUTH"))
                {
                    Console.WriteLine($"Received: {message}");
                    var writer = new StreamWriter(client.GetStream()) { AutoFlush = true };
                    writer.WriteLine("REPLY OK IS Authenticated");
                    BroadcastMessage("MSG FROM Server IS New User Joined", null);
                    continue;
                }
                else
                {
                    Console.WriteLine($"Received: {message}");
                    BroadcastMessage(message, client);
                }
            }
        }
        catch (IOException) // Catch exceptions when client disconnects unexpectedly
        {
            BroadcastMessage("MSG FROM Server IS User Left", null);
        }
        finally
        {   
            _clients.Remove(client);
            client.Close();
            BroadcastMessage("MSG FROM Server IS User Left", null);
        }
    }

    
    private void HandleServerInput()
    {
        while (true)
        {
            var message = Console.ReadLine();
            if (!string.IsNullOrEmpty(message))
            {
                foreach (var client in _clients)
                {
                    if (client.Connected)
                    {
                        var writer = new StreamWriter(client.GetStream()) { AutoFlush = true };
                        writer.WriteLine(message);
                    }
                }
            }
        }
    }

    private void BroadcastMessage(string message, TcpClient sender)
    {
        lock (_clientsLock)
        {
            foreach (var client in _clients.ToList()) 
            {
                if (client.Connected)
                {
                    if(client == sender || !message.Contains("MSG FROM")) continue;
                    var writer = new StreamWriter(client.GetStream()) { AutoFlush = true };
                    writer.WriteLine(message);
                }
                else
                {
                    _clients.Remove(client);
                }
            }
        }
    }

}