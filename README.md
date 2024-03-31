# IPK24-CHAT Client

This repository contains the source code for the IPK24-CHAT client application. The application supports communication over TCP and UDP protocols, allowing users to send and receive messages in a chat-like interface.

## Files

- `argumentParsing.cs`: Handles command-line argument parsing to configure the chat client.
- `TCP.cs`: Implements the TCP chat client logic, including connecting to a server, sending, and receiving messages.
- `UDP.cs`: Implements the UDP chat client logic, including connecting to a server, sending, and receiving messages with additional mechanisms for confirmations and retries.
- `UDPmessageHelper.cs`: Provides utilities for constructing and parsing UDP messages according to the application's protocol.

## Features

- Connect to a chat server using either TCP or UDP protocol.
- Authenticate with a server using a username, secret, and display name.
- Join chat channels.
- Send and receive chat messages.
- Rename the display name.
- Handle server responses and errors.

## Usage

1. Compile the source code using your preferred C# compiler or IDE.
2. Run the compiled application from the command line with the necessary arguments.

### Command-Line Arguments

- `--transport-protocol` or `-t`: Specify the transport protocol (`tcp` or `udp`). This argument is required.
- `--server-address` or `-s`: Specify the server's IP address or hostname. This argument is required.
- `--server-port` or `-p`: Specify the server port. The default port is `4567`.
- `--udp-confirmation-timeout` or `-d`: Specify the UDP confirmation timeout in milliseconds. The default is `250` ms.
- `--udp-retry-count` or `-r`: Specify the maximum number of UDP retransmissions. The default is `3`.

### Example for TCP

```shell
./ipk24chat-client -t tcp -s anton5.fit.vutbr.cz -p 4567 
```
### Example for UDP

```shell
./ipk24chat-client -t udp -s anton5.fit.vutbr.cz -p 4567 -d 500 -r 3
```
### In-Application Commands

- `/auth {Username} {Secret} {DisplayName}`: Authenticate with the server.
- `/join {ChannelID}`: Join a chat channel.
- `/rename {DisplayName}`: Change your display name.
- `/bye`: Disconnect from the server.
- `/help`: Show a list of available commands.

## Dependencies

The application uses the `System.CommandLine` library for argument parsing and `System.Net.Sockets` for network communication.
