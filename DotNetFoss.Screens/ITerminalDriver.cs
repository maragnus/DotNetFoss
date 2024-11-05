namespace DotNetFoss.Screens;

public interface ITerminalDriver
{
    Stream OpenOutputStream();
    void HideCursor();
    void ShowCursor();
    void EnableEcho();
    void DisableEcho();
}