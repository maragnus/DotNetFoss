using DotNetFoss.DoorConnections;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;

namespace DotNetFoss.DropFiles;

public class Door32Sys : IDropFile, IDoorSession
{
    private const string DoorSys32FileName = "door32.sys";

    private readonly ILogger _logger;

    private readonly DoorConnectionFactory _factory;

    public DateTimeOffset SessionExpiration { get; private set; }

    public int? UserId { get; private set; }

    public string? UserName { get; private set; }

    public string? RealName { get; private set; }

    public string? BbsName { get; private set; }

    public Emulation Emulation { get; private set; }

    public object? DropFile { get; private set; }

    public IDoorConnection Connection { get; private set; } = null!;

    public Door32Sys(ILogger<Door32Sys> logger, DoorConnectionFactory factory)
    {
        _logger = logger;
        _factory = factory;
    }

    public async Task<IDoorSession?> InitializeAsync(string dropFilePath)
    {
        try
        {
            if (dropFilePath == null)
                return null;

            if (!string.Equals(Path.GetFileName(dropFilePath), DoorSys32FileName, StringComparison.InvariantCultureIgnoreCase))
                return null;

            var dropFile = await GetDropFile(dropFilePath);
            if (dropFile == null)
                return null;

            SessionExpiration = DateTimeOffset.UtcNow + dropFile.TimeRemaining;
            UserId = dropFile.UserId;
            UserName = dropFile.UserHandle;
            RealName = dropFile.RealName;
            BbsName = dropFile.BbsId;
            Emulation = dropFile.Emulation;
            DropFile = dropFile;

            switch (dropFile.CommType)
            {
                case CommType.Local:
                    throw new NotImplementedException("CommType of Local in DOOR32.SYS is not supported");

                case CommType.Serial:
                    Connection = _factory.CreateSerial($"COM{dropFile.CommHandle}", dropFile.BaudRate);
                    return this;

                case CommType.Telnet:
                    Connection = _factory.CreateSocket((IntPtr)dropFile.CommHandle);
                    return this;

                default:
                    _logger.LogWarning($"{nameof(Door32Sys)} found unsupported CommType in {DoorSys32FileName} of {dropFile.CommType}");
                    return null;
            }
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.NotSocket)
        {
            _logger.LogWarning($"{nameof(Door32Sys)} could not connect, socket expired");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"{nameof(Door32Sys)} could not connect via {DoorSys32FileName}");
            return null;
        }
    }

    async Task<Door32SysFile?> GetDropFile(string dropFilePath)
    {
        if (!File.Exists(dropFilePath))
        {
            _logger.LogInformation($"{nameof(Door32Sys)} did not file {dropFilePath}");
            return null;
        }

        try
        {
            using var file = new StreamReader(dropFilePath);
            return new Door32SysFile
            {
                CommType = (CommType)await ReadInt(),
                CommHandle = await ReadInt(),
                BaudRate = await ReadInt(),
                BbsId = await ReadString(),
                UserId = await ReadInt(),
                RealName = await ReadString(),
                UserHandle = await ReadString(),
                SecurityLevel = await ReadInt(),
                TimeRemaining = TimeSpan.FromMinutes(await ReadInt()),
                Emulation = (Emulation)await ReadInt(),
                CurrentNodeNumber = await ReadInt(),
            };

            async Task<int> ReadInt() => int.Parse((await file.ReadLineAsync())!);
            async Task<string> ReadString() => (await file.ReadLineAsync()) ?? "";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"{nameof(Door32Sys)} could not parse {dropFilePath}");
            return null;
        }
    }
}

public enum CommType : int
{
    Local = 0,
    Serial = 1,
    Telnet = 2,
}

public record Door32SysFile
{
    public CommType CommType { get; init; } = CommType.Local;
    public int CommHandle { get; init; }
    public int BaudRate { get; init; }
    public string BbsId { get; init; } = "";
    public int UserId { get; init; }
    public string RealName { get; init; } = "";
    public string UserHandle { get; init; } = "";
    public int SecurityLevel { get; init; }
    public TimeSpan TimeRemaining { get; init; } = TimeSpan.Zero;
    public Emulation Emulation { get; init; }
    public int CurrentNodeNumber { get; init; }
}
