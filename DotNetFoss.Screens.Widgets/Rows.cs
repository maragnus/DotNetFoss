using System.Drawing;

namespace DotNetFoss.Screens.Widgets;

public class Rows : Container
{
    public override void PerformLayout(LayoutContext context)
    {
        context.Stack.Push(this);
        Bounds = context.ClientArea;
        var columnHeight = Bounds.Height / Children.Length;
        var remainingHeight = Bounds.Height % Children.Length;
        var top = Bounds.Top;
        for (var i = 0; i < Children.Length; i++)
        {
            var height = columnHeight + (remainingHeight-- > 0 ? 1 : 0);
            var bounds = new Rectangle(Bounds.Left, top, Bounds.Width, height);
            top += height;
            context.ClientArea = bounds;
            Children[i].PerformLayout(context);
        }
        context.Stack.Pop();
    }
}