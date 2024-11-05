using System.Drawing;

namespace DotNetFoss.Screens.Widgets;

public class Border : Widget
{
    public BorderStyle Style { get; init; } = BorderStyle.None;
    public Widget? Child { get; init; }
    public string? Label { get; init; }

    public override void PerformLayout(LayoutContext context)
    {
        Bounds = context.ClientArea;
        if (Child is null || Style == BorderStyle.None) return;
        context.Stack.Push(this);
        context.ClientArea = new Rectangle(Bounds.Left + 1, Bounds.Top + 1, Bounds.Width - 2, Bounds.Height - 2);
        context.Stack.Pop();
    }

    public override void Render(RenderContext context)
    {
        if (Bounds.IsEmpty) return;

        var sb = context.ScreenBuffer;

        switch (Style)
        {
            case BorderStyle.Rounded:
                sb.Box(Bounds, '\u256d', '\u256e', '\u2570', '\u256f', '\u2500', '\u2502');
                break;
            case BorderStyle.Double:
                sb.Box(Bounds, '\u2554', '\u2557', '\u255a', '\u255d', '\u2550', '\u2551');
                break;
            case BorderStyle.Thin:
                sb.Box(Bounds, '\u250c', '\u2510', '\u2514', '\u2518', '\u2500', '\u2502');
                break;
        }

        sb.Fill(Rectangle.Inflate(Bounds, -1, -1), 'X');
        
        if (string.IsNullOrWhiteSpace(Label)) return;
        
        sb.MoveTo(Bounds.Left + 4, Bounds.Top);
        sb.Text("[ ");
        sb.Text(Label);
        sb.Text(" ]");
    }
}