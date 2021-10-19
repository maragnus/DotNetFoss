using DotNetFoss.DoorConnections;
using Microsoft.Extensions.Logging;

namespace DotNetFoss.DropFiles
{
    public class DoorSys : IDropFile, IDoorSession
    {
        private const string DoorSysFileName = "door.sys";

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

        public DoorSys(ILogger<Door32Sys> logger, DoorConnectionFactory factory)
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

                if (!string.Equals(Path.GetFileName(dropFilePath), DoorSysFileName, StringComparison.InvariantCultureIgnoreCase))
                    return null;

                var dropFile = await GetDropFile(dropFilePath);
                if (dropFile == null)
                    return null;

                SessionExpiration = DateTimeOffset.UtcNow + TimeSpan.FromMinutes(dropFile.MinutesRemaining);
                UserId = dropFile.UserFileRecordNumber;
                UserName = dropFile.UserFullName;
                RealName = dropFile.UserFullName;
                BbsName = "";
                Emulation = Emulation.Ascii;
                DropFile = dropFile;

                Connection = _factory.CreateSerial(dropFile.CommPort, dropFile.BaudRate);

                return this;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"{nameof(DoorSys)} could not connect via {DoorSysFileName}");
                return null;
            }
        }
        async Task<DoorSysFile?> GetDropFile(string dropFilePath)
        {
            if (!File.Exists(dropFilePath))
            {
                _logger.LogInformation($"{nameof(DoorSys)} did not file {dropFilePath}");
                return null;
            }

            try
            {
                using var file = new StreamReader(dropFilePath);
                return new DoorSysFile
                {
                    CommPort = await ReadString(),
                    BaudRate = await ReadInt(),
                    Parity = await ReadInt(),
                    CurrentNodeNumber = await ReadInt(),
                    LockedAt19200 = await ReadBool(),
                    ScreenDisplay = await ReadBool(),
                    PrinterToggle = await ReadBool(),
                    PageBell = await ReadBool(),
                    CallerAlarm = await ReadBool(),
                    UserFullName = await ReadString(),
                    CallingFrom = await ReadString(),
                    HomePhone = await ReadString(),
                    WorkDataPhone = await ReadString(),
                    Password = await ReadString(),
                    SecurityLevel = await ReadInt(),
                    TotalTimesOn = await ReadInt(),
                    LastDateCalled = await ReadString(),
                    SecondsRemaining = await ReadInt(),
                    MinutesRemaining = await ReadInt(),
                    GraphicsMode = await ReadString(),
                    PageLength = await ReadInt(),
                    UserMode = await ReadChar(),
                    RegisteredIn = await ReadString(),
                    ExitTo = await ReadString(),
                    ExpirationDate = await ReadString(),
                    UserFileRecordNumber = await ReadInt(),
                    DefaultProtocol = await ReadChar(),
                    TotalUploads = await ReadInt(),
                    TotalDownloads = await ReadInt(),
                    DailyDownloadKTotal = await ReadInt(),
                    DailyDownloadMaxKLimit = await ReadInt(),
                };

                async Task<int> ReadInt() => int.Parse((await file.ReadLineAsync())!);
                async Task<string> ReadString() => (await file.ReadLineAsync()) ?? "";
                async Task<char> ReadChar() => ((await file.ReadLineAsync()) ?? " ")[0];
                async Task<bool> ReadBool() => ((await file.ReadLineAsync()) ?? "").ToLowerInvariant() == "Y";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"{nameof(DoorSys)} could not parse {DoorSysFileName}");
                return null;
            }
        }
    }

    public record DoorSysFile
    {
        public string CommPort { get; init; } = "";
        public int BaudRate { get; init; }
        public int Parity { get; init; }
        public int CurrentNodeNumber { get; init; }
        public bool LockedAt19200 { get; init; }
        public bool ScreenDisplay { get; init; }
        public bool PrinterToggle { get; init; }
        public bool PageBell { get; init; }
        public bool CallerAlarm { get; init; }
        public string UserFullName { get; init; } = "";
        public string CallingFrom { get; init; } = "";
        public string HomePhone { get; init; } = "";
        public string WorkDataPhone { get; init; } = "";
        public string Password { get; init; } = "";
        public int SecurityLevel { get; init; }
        public int TotalTimesOn { get; init; }
        public string LastDateCalled { get; init; } = "";
        public int SecondsRemaining { get; init; }
        public int MinutesRemaining { get; init; }
        public string GraphicsMode { get; init; } = "";
        public int PageLength { get; init; }
        public char UserMode { get; init; }
        public string RegisteredIn { get; init; } = "";
        public string ExitTo { get; init; } = "";
        public string ExpirationDate { get; init; } = "";
        public int UserFileRecordNumber { get; init; }
        public char DefaultProtocol { get; init; }
        public int TotalUploads { get; init; }
        public int TotalDownloads { get; init; }
        public int DailyDownloadKTotal { get; init; }
        public int DailyDownloadMaxKLimit { get; init; }
    }
}
