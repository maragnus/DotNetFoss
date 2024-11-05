namespace DotNetFoss.Screens;

public static class ScreenExtensions
{
    public static void WriteText(this IScreenBuffer screenBuffer, int column, int row, string text)
    {
        screenBuffer.MoveTo(column, row);
        screenBuffer.Text(text);
    }

    public static IScreenBuffer CreateScreenBuffer(this ConsoleTerminalDriver terminalDriver)
    {
        return AnsiScreenBuffer.Create(terminalDriver, Console.WindowWidth, Console.WindowHeight);
    }
}