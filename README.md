# IPK24-CHAT Client Documentation

## Table of Contents
- [Introduction](#introduction)
- [Theoretical Background](#theoretical-background)
- [Architecture and Design](#architecture-and-design)
  - [UML Diagrams](#uml-diagrams)
  - [Interesting Code Sections](#interesting-code-sections)
- [Testing](#testing)
  - [Test Environment](#test-environment)
  - [Test Cases](#test-cases)
  - [Comparison with Similar Tools](#comparison-with-similar-tools)
- [Extra Features](#extra-features)
- [Bibliography](#bibliography)
- [Usage](#usage)
- [License](#license)
- [Client Documentation Summary](#client-documentation-summary)

## Introduction
The IPK24-CHAT client application supports communication over TCP and UDP protocols, offering a chat-like interface for message exchange. This document provides an executive summary of the application, details its architecture, presents testing strategies, and outlines extra features.

## Theoretical Background
To understand the IPK24-CHAT client, familiarity with the following concepts is necessary:
- TCP/UDP Protocols: The application leverages both Transmission Control Protocol (TCP) and User Datagram Protocol (UDP) for message transmission.
- Client-Server Model: The application operates within this model, connecting to a server for chat functionalities.
- System.CommandLine and System.Net.Sockets: These libraries are crucial for argument parsing and network communication, respectively.

## Architecture and Design
### UML Diagrams
![Mermaid Diagram](https://www.mermaidchart.com/raw/92699b87-3e93-40a2-9b16-b88669be3246?theme=dark&version=v0.1&format=svg)

### Interesting Code Sections
- **Argument Parsing**: `argumentParsing.cs` utilizes the `System.CommandLine` library for command-line argument handling.
- **TCP and UDP Logic**: `TCP.cs` and `UDP.cs` contain the core functionality for their respective protocols, showcasing the use of `System.Net.Sockets`.

## Testing
### Test Environment
- **Network Topology**: Testing was conducted on a home network with a client and server application running on separate machines.
- **Hardware Specification**: Client - Windows 11 PC with 8GB RAM; Server - Ubuntu 20.04.
- **Software Versions**: .NET 8.0 SDK.
### Test Cases
#### TCP Connection
- **Objective**: Validate TCP-based message sending and receiving.
- **Procedure**: Run the client with `-t tcp` and send messages to a running server.
- **Expected Output**: Messages are echoed back by the server.
- **Actual Output**: As expected.
#### UDP Message Retransmission
- **Objective**: Test UDP retry logic on message failure.
- **Procedure**: Disconnect the server after the first message and observe client retries.
- **Expected Output**: Client attempts retransmission up to the specified retry count.
- **Actual Output**: As expected.
### Comparison with Similar Tools
Compared to a standard chat application like IRC, IPK24-CHAT offers a simpler interface but includes robust TCP and UDP support, highlighting its reliability over UDP with custom retransmission logic.

## Extra Features
- **UDP Retransmission**: Beyond the basic UDP send-receive, this application implements a retry mechanism to ensure message delivery.
- **Command-Line Flexibility**: Enhanced parsing logic allows for dynamic command-line configuration, adapting to various network settings.

## Bibliography
- Tanenbaum, A. S., & Wetherall, D. J. (2011). Computer Networks. Prentice Hall.
- Microsoft .NET Documentation for `System.CommandLine` and `System.Net.Sockets`.

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
./ipk24chat-client -t tcp -s [server_address] -p [port]
```
### Example for UDP

```shell
./ipk24chat-client -t udp -s [server_address] -p [port] -d 500 -r 3
```
### In-Application Commands

- `/auth {Username} {Secret} {DisplayName}`: Authenticate with the server.
- `/join {ChannelID}`: Join a chat channel.
- `/rename {DisplayName}`: Change your display name.
- `/bye`: Disconnect from the server.
- `/help`: Show a list of available commands.

## License

The IPK24-CHAT Client is open-source software licensed under the MIT License. This license permits use, modification, and distribution of the software under specific conditions, promoting a collaborative and open-source approach to software development.

For detailed license terms, please refer to the [LICENSE](./LICENSE) file included with this project.

### Key Points of the GPL-3.0 License:

- **Freedom** is ensured for all to use, study, share, and modify the software, adhering to the principles of copyleft under the following conditions:
  - Modifications and derived works must also be licensed under GPL-3.0, ensuring future users have the same freedoms.
  - Any distribution, including modified versions, must include the original GPL-3.0 license, any applicable notices, and make the source code accessible.
  - THE SOFTWARE IS DISTRIBUTED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, ensuring users understand it is provided without guarantees.


### Contributions

We warmly welcome your contributions, which will be accepted under the same GPL-3.0 license. By participating in this project, you consent to respect and follow its guidelines and stipulations.

### More Information

Opting for the GPL-3.0 license promotes widespread engagement and utilization, guaranteeing that the IPK24-CHAT Client stays freely available to a diverse audience. For comprehensive details of the license and for any questions concerning its application, please consult the [LICENSE](./LICENSE) file included in the project repository.


## Client Documentation Summary

### Key Highlights

- **Protocol Flexibility**: Supports both **TCP** and **UDP** protocols, allowing for reliable or faster message delivery as needed.
- **Robust Testing**: Includes a comprehensive testing framework, covering both basic functionalities and edge cases, ensuring the application's reliability.
- **Unique Features**: Introduces UDP message retransmission and adaptive command-line configurations, enhancing usability and reliability.
- **Comprehensive Documentation**: Features detailed sections on theoretical background, system architecture, and practical usage, complete with UML diagrams and code insights.

### Getting Started

### Prerequisites
- .NET SDK (version specified in documentation)
