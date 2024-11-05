namespace DotNetFoss.Screens;

public class ConsoleTerminalDriver : ITerminalDriver
{
    public Stream OpenOutputStream() => Console.OpenStandardOutput();

    public void HideCursor() => Console.CursorVisible = false;

    public void ShowCursor() => Console.CursorVisible = true;

    public void EnableEcho()
    {
        // Not supported
    }

    public void DisableEcho()
    {
        // Not supported
    }
}