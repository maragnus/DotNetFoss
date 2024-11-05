using System.Drawing;

namespace DotNetFoss.Screens.Widgets;

public class LayoutContext
{
    public Rectangle ScreenArea { get; set; }
    public Rectangle ClientArea { get; set; }
    public Stack<Widget> Stack { get; } = [];
}