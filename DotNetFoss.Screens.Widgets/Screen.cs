using System.Drawing;

namespace DotNetFoss.Screens.Widgets;

public class Screen
{
    public required Widget Root { get; init; }

    public void Render(IScreenBuffer screenBuffer)
    {
        var screenRect = new Rectangle(0, 0, screenBuffer.Columns, screenBuffer.Rows);
        var layoutContext = new LayoutContext {ScreenArea = screenRect, ClientArea = screenRect};
        Root.PerformLayout(layoutContext);
        
        var renderContext = new RenderContext { ScreenBuffer = screenBuffer };
        Root.Render(renderContext);
    }
}