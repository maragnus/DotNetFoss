using Microsoft.Extensions.Logging;
using System.IO.Ports;
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

    internal IDoorConnection CreateSerial(string comPort, int baudRate)
    {
        var serialPort = new SerialPort(comPort, baudRate);
        return new SerialPortDoorConnection(serialPort, _loggerFactory.CreateLogger<SerialPortDoorConnection>());
    }
}
