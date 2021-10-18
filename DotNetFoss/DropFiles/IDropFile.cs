using DotNetFoss.DoorConnections;

namespace DotNetFoss.DropFiles;

public interface IDoorSession
{
    public IDoorConnection Connection { get; }
    public DateTimeOffset SessionExpiration { get; }
    public int? UserId { get; }
    public string? UserName { get; }
    public string? RealName { get; }
    public string? BbsName { get; }
    public Emulation Emulation { get; }
    public object? DropFile { get; }
}

public interface IDropFile
{
    /// <summary>
    /// Initialize this DoorLink method, if succcessful, it will be used to interact with the BBS Host
    /// </summary>
    /// <returns>If this link has been established, returns the session.</returns>
    public Task<IDoorSession?> InitializeAsync();
}
