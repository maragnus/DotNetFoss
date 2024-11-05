using System.Drawing;

namespace DotNetFoss.Screens;

public interface IScreenBuffer
{
    ITerminalDriver TerminalDriver { get; }
    string Title { get; set; }
    int Columns { get; }
    int Rows { get; }

    void SetResolution(int width, int height);
    void MoveTo(int column, int row);
    void Text(string text);
    void Fill(Rectangle bounds, char character);
    void Box(Rectangle bounds, char tl, char tr, char bl, char br, char h, char v);

    void Render();

    void ResetStyle();
    void ResetForeground();
    void ResetBackground();
    void SetForeground(byte red, byte green, byte blue);
    void SetForeground(byte index);
    void SetForeground(AnsiColor color);
    void SetBackground(byte red, byte green, byte blue);
    void SetBackground(byte index);
    void SetBackground(AnsiColor color);
    void SetBold(bool isSet);
    void SetFaint(bool isSet);
    void SetItalic(bool isSet);
    void SetUnderline(bool isSet);
    void SetBlink(bool isSet);
    void SetInverse(bool isSet);
    void SetStrikeout(bool isSet);
    void SetHidden(bool isSet);
}