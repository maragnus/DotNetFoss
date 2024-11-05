namespace DotNetFoss.Screens.Widgets;

public abstract class Container : Widget
{
    public Widget[] Children { get; init; } = [];

    public override void PerformLayout(LayoutContext context)
    {
        base.PerformLayout(context);
        context.Stack.Push(this);
        foreach (var child in Children)
        {
            context.ClientArea = Bounds;
            child.PerformLayout(context);
        }
        context.Stack.Pop();
    }

    public override void Render(RenderContext context)
    {
        context.Stack.Push(this);
        foreach (var child in Children)
        {
            child.Render(context);
        }
        context.Stack.Pop();
    }
}