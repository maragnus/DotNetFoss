using Microsoft.Extensions.Logging;
using System.Net.Sockets;

namespace DotNetFoss.DoorConnections;

public class DoorConnectionFactory
{
    private readonly ILoggerFactory _loggerFactory;

    public DoorConnectionFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    public IDoorConnection CreateSocket(IntPtr socketHandle)
    {
        var socket = new Socket(new SafeSocketHandle(socketHandle, ownsHandle: false));
        return new SocketDoorConnection(socket, _loggerFactory.CreateLogger<SocketDoorConnection>());
    }
}
