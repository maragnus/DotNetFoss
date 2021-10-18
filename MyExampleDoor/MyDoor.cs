using DotNetFoss;
using DotNetFoss.DoorConnections;
using DotNetFoss.DropFiles;
using Microsoft.Extensions.Logging;

public class MyDoor : IDoorApplication
{
    private readonly IDoorSession _session;
    private readonly IDoorConnection _connection;
    private readonly ILogger<MyDoor> _logger;

    public MyDoor(IDoorSession session, IDoorConnection connection, ILogger<MyDoor> logger)
    {
        _session = session;
        _connection = connection;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        _connection.Write($"Hello, {_session.UserName}!");

        try
        {
            while (true)
            {
                await Task.Yield();

                var ch = _connection.ReadChar();

                if (ch == null)
                    continue;

                _connection.Write(ch.Value);
                if (ch == '\r')
                    _connection.Write('\n');

                if (ch == 'q')
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "App failed");
        }
    }
}