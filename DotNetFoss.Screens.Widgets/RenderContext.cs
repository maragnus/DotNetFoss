namespace DotNetFoss.Screens.Widgets;

public class RenderContext
{
    public required IScreenBuffer ScreenBuffer { get; init; }
    public Stack<Widget> Stack { get; } = [];
}