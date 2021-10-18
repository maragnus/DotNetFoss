
namespace DotNetFoss.DoorConnections;

public interface IDoorConnection
{
    CancellationToken CancellationToken { get; }

    Task StartAsync();

    /// <summary>
    /// Returns next <see cref="System.Char"/> waiting in the buffer.
    /// </summary>
    /// <returns>Return next character or null if nothing is waiting</returns>
    char? ReadChar();

    /// <summary>
    /// Returns all characters waiting in the buffer.
    /// </summary>
    /// <returns>Returns a string or null if nothing is waiting</returns>
    string? Read();

    void Write(string? characters);

    void Write(char? character)
    {
        if (character == null)
            return;
        Write(character.Value);
    }

    void Write(char character);

    void Disconnect();
}
