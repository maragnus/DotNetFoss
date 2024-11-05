using System.Drawing;

namespace DotNetFoss.Screens.Widgets;

public class Columns : Container
{
    public override void PerformLayout(LayoutContext context)
    {
        context.Stack.Push(this);
        Bounds = context.ClientArea;
        var columnWidth = Bounds.Width / Children.Length;
        var remainingWidth = Bounds.Width % Children.Length;
        var left = Bounds.Left;
        for (var i = 0; i < Children.Length; i++)
        {
            var width = columnWidth + (remainingWidth-- > 0 ? 1 : 0);
            var bounds = new Rectangle(left, Bounds.Top, width, Bounds.Height);
            left += width;
            context.ClientArea = bounds;
            Children[i].PerformLayout(context);
        }
        context.Stack.Pop();
    }
}