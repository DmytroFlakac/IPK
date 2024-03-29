// using System.Net;
// using System.Net.Sockets;
// using System.Text;

// namespace IPK24Chat
// {
//     class UDPlistenServer
//     {
//         public static async Task ListenForServer(UdpClient client, CancellationToken cts)
//         {
//             IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.Any, 0);
//             try
//             {
//                 while (!cts.IsCancellationRequested)
//                 {                    
//                     UdpReceiveResult result = await client.ReceiveAsync();
//                     byte[] message = result.Buffer;
//                     serverEndpoint = result.RemoteEndPoint;
//                     int messageID = UDPmessageHelper.getMessageID(message);
//                     MessageType messageType = UDPmessageHelper.getMessageType(message);                   
//                 }
//             }
//             catch (SocketException e)
//             {
//                 Console.Error.WriteLine($"SocketException: {e.Message}");
//             }
//         }

    
//     }
// }