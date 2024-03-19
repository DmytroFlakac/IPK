using System.CommandLine;
using System.CommandLine.Invocation;


namespace IPK24Chat;
class Program
{
    static void Main(string[] args)
    {
        var transportProtocolOption = new Option<string>(
            aliases: new[] { "--transport-protocol", "-t" },
            description: "Transport protocol (tcp or udp)")
        {
            IsRequired = true 
        };

        var serverAddressOption = new Option<string>(
            aliases: new[] { "--server-address", "-s" },
            description: "Server IP or hostname")
        {
            IsRequired = true 
        };

        var serverPortOption = new Option<int>(
            aliases: new[] { "--server-port", "-p" },
            getDefaultValue: () => 4567,
            description: "Server port");

        var udpConfirmationTimeoutOption = new Option<int>(
            aliases: new[] { "--udp-confirmation-timeout", "-d" },
            getDefaultValue: () => 250,
            description: "UDP confirmation timeout in ms");

        var udpRetryCountOption = new Option<int>(
            aliases: new[] { "--udp-retry-count", "-r" },
            getDefaultValue: () => 3,
            description: "Maximum number of UDP retransmissions");

        var rootCommand = new RootCommand("IPK24-CHAT client");
        rootCommand.AddOption(transportProtocolOption);
        rootCommand.AddOption(serverAddressOption);
        rootCommand.AddOption(serverPortOption);
        rootCommand.AddOption(udpConfirmationTimeoutOption);
        rootCommand.AddOption(udpRetryCountOption);

        rootCommand.SetHandler(
            (transportProtocol, serverAddress, serverPort, udpConfirmationTimeout, udpRetryCount) =>
            {
                if (transportProtocol == "tcp")
                {
                    TcpChatClient tcpClient = new TcpChatClient();
                    tcpClient.Connect(serverAddress, serverPort);
                    tcpClient.StartInteractiveSession(); 
                }
                else if (transportProtocol == "udp")
                {
                    // UdpChatClient udpClient = new UdpChatClient();
                    // udpClient.Connect(serverAddress, serverPort, udpConfirmationTimeout, udpRetryCount);
                    // ... (UDP client logic)
                }
                else
                {
                    Console.WriteLine("Invalid transport protocol specified.");
                }
            }, transportProtocolOption, serverAddressOption, serverPortOption,
            udpConfirmationTimeoutOption, udpRetryCountOption);

        // Parse command-line arguments and execute the root command
        rootCommand.Invoke(args);
    }
}
