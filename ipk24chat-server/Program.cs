using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Threading.Tasks;

namespace Server;

class Program
{
    static async Task Main(string[] args)
    {
        // Server listening IP address
        var serverIPOption = new Option<string>(
            "--listen-ip",
            () => "0.0.0.0",
            "Server listening IP address for welcome sockets")
        {
            Name = "-l",
            IsRequired = false // Default value provided
        };

        // Server listening port
        var serverPortOption = new Option<int>(
            "--listen-port",
            () => 4567,
            "Server listening port for welcome sockets")
        {
            Name = "-p",
            IsRequired = false // Default value provided
        };

        // UDP confirmation timeout in ms
        var udpConfirmationTimeoutOption = new Option<int>(
            "--udp-confirmation-timeout",
            () => 250,
            "UDP confirmation timeout in ms")
        {
            Name = "-d",
            IsRequired = false // Default value provided
        };

        // Maximum number of UDP retransmissions
        var udpRetryCountOption = new Option<byte>(
            "--udp-retry-count",
            () => 3,
            "Maximum number of UDP retransmissions")
        {
            Name = "-r",
            IsRequired = false // Default value provided
        };

        // Help option
        var helpOption = new Option<bool>(
            "--help",
            "Prints program help output and exits")
        {
            Name = "-h",
            IsRequired = false
        };

        var rootCommand = new RootCommand("Server application for IPK24-CHAT")
        {
            serverIPOption,
            serverPortOption,
            udpConfirmationTimeoutOption,
            udpRetryCountOption,
            helpOption
        };

        rootCommand.Handler = CommandHandler.Create<string, int, int, byte, bool>(async (listenIp, listenPort, udpConfirmationTimeout, udpRetryCount, help) =>
        {
            if (help)
            {
                Console.WriteLine(rootCommand.Description);
                foreach (var option in rootCommand.Options)
                {
                    Console.WriteLine($"{option.Name}, {option.Description}");
                }
                return;
            }

            var users = new Dictionary<string, List<User>>();
            TcpServer tcpServer = new TcpServer(listenIp, listenPort, users);
            var tcpServerTask = tcpServer.Start();
            UdpServer udpServer = new UdpServer(listenIp, listenPort, users, udpConfirmationTimeout, udpRetryCount);
            var udpServerTask = udpServer.Start();
    
            // Now that the lambda is marked as async, await can be used.
            await Task.WhenAll(tcpServerTask, udpServerTask);
        });
        await rootCommand.InvokeAsync(args);
    }
}
