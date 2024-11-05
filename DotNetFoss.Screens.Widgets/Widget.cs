using System.Drawing;

namespace DotNetFoss.Screens.Widgets;

public abstract class Widget
{
    public string? Name { get; init; }
    public Rectangle Bounds { get; protected set; }

    public virtual void PerformLayout(LayoutContext context)
    {
        Bounds = context.ClientArea;
    } 
    
    public virtual void Render(RenderContext context) {}
}