using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
namespace DotNetFoss.Screens;

public class AnsiScreenBuffer : IScreenBuffer
{
    private const bool DebugOutput = false;
    private const string Escape = DebugOutput ? "^[" : "\u001b[";

    [Flags]
    public enum CellStyle : byte
    {
        Normal = 0,
        Bold = 1,
        Faint = 2,
        Italic = 4,
        Underline = 8,
        Blink = 16,
        Inverse = 32,
        Hidden = 64,
        Strikeout = 128
    }

    [StructLayout(LayoutKind.Explicit, Pack = 8, Size = 8)]
    private struct Cell
    {
        public static Cell Empty =>
            new() { Character = ' ', Foreground = 0, Background = 0, Style = CellStyle.Normal };

        [FieldOffset(0)] public char Character;
        [FieldOffset(2)] public short Foreground;
        [FieldOffset(4)] public short Background;
        [FieldOffset(6)] public CellStyle Style;
    }

    public int Columns { get; private set; }
    public int Rows { get; private set; }
    public Encoding Encoding { get; } = Encoding.UTF8;
    public bool CursorVisible { get; set; }
    public ITerminalDriver TerminalDriver { get; }

    private int _cursorColumn = -1;
    private int _cursorRow = -1;
    private Cell _cursor = Cell.Empty;
    private Cell[] _screenBuffer = [];
    private Cell[] _nextFrame = [];
    private readonly List<(int start, int end)> _changes = new(100);
    private readonly List<string> _styles = [];
    private readonly AnsiPalette _foregroundPalette;
    private readonly AnsiPalette _backgroundPalette;
    private string? _lastTitle;

    private int[] PurgeForeground()
    {
        var usages = new int[AnsiPalette.PaletteSize];
        foreach (var cell in _nextFrame)
            usages[cell.Foreground]++;
        return usages;
    }

    private int[] PurgeBackground()
    {
        var usages = new int[AnsiPalette.PaletteSize];
        foreach (var cell in _nextFrame)
            usages[cell.Background]++;
        return usages;
    }

    public static IScreenBuffer Create(ITerminalDriver terminalDriver, int width, int height)
    {
        var screenBuffer = new AnsiScreenBuffer(terminalDriver);
        screenBuffer.SetResolution(width, height);
        return screenBuffer;
    }

    private AnsiScreenBuffer(ITerminalDriver terminalDriver)
    {
        TerminalDriver = terminalDriver;
        _foregroundPalette = new AnsiPalette(PurgeForeground, "39");
        _backgroundPalette = new AnsiPalette(PurgeBackground, "49");
    }

    public void MoveTo(int column, int row)
    {
        // Console.WriteLine($"MOVETO {column}, {row}");
        _cursorRow = row;
        _cursorColumn = column;
    }

    public void Text(char character)
    {
        // Console.WriteLine($"TEXTAT {_cursorColumn}, {_cursorRow} = {character}");

        var index = GetCursorIndex(_cursorColumn, _cursorRow);
        _nextFrame[index] = _cursor with { Character = character };
        _cursorColumn++;
        if (_cursorColumn >= Columns)
        {
            _cursorColumn = 0;
            _cursorRow++;
        }
    }

    public void Text(string text)
    {
        foreach (var character in text)
            Text(character);
    }

    public void Fill(Rectangle bounds, char character)
    {
        var cursor = _cursor with { Character = character };
        for (int r = bounds.Top; r < bounds.Bottom; r++)
        for (int c = bounds.Left; c < bounds.Right; c++)
            _nextFrame[GetCursorIndex(c, r)] = cursor;
    }
    
    public void Box(Rectangle bounds, char tl, char tr, char bl, char br, char h, char v)
    {
        MoveTo(bounds.Left, bounds.Top);
        Text(tl);
        for (var c = 0; c < bounds.Width - 2; c++)
            Text(h);
        Text(tr);

        for (var r = bounds.Top + 1; r < bounds.Bottom - 1; r++)
        {
            MoveTo(bounds.Left, r);
            Text(v);
            MoveTo(bounds.Right - 1, r);
            Text(v);
        }
        
        MoveTo(bounds.Left, bounds.Bottom-1);
        Text(bl);
        for (var c = 0; c < bounds.Width - 2; c++)
            Text(h);
        Text(br);
    }


    public void ResetCursor()
    {
        _cursorColumn = -1;
        _cursorRow = -1;
        _cursor = Cell.Empty;
    }

    public void SetResolution(int columns, int rows)
    {
        Columns = columns;
        Rows = rows;
        _screenBuffer = new Cell[columns * rows];
        _nextFrame = new Cell[columns * rows];
        Array.Fill(_screenBuffer, Cell.Empty);
        Array.Fill(_nextFrame, Cell.Empty);
    }
    
    public void Render()
    {
        using var writer = new StreamWriter(TerminalDriver.OpenOutputStream(), Encoding);

        SetCursorVisible(writer, false);
        TerminalDriver.DisableEcho();
  
        IdentifyChanges();
        ResetCursor(); 
        
        UpdateTitle(writer);
        foreach (var (start, end) in _changes)
            Write(writer, start, end);
        UpdateStyle(writer, _cursor);
        MoveCursor(writer, _cursorColumn, _cursorRow);
        if (CursorVisible)
            SetCursorVisible(writer, true);
    }

    private void UpdateTitle(StreamWriter writer)
    {
        if (_lastTitle == Title) return; 
        writer.Write($"\u001b]0;{Title}\u001b\\");
        writer.Write($"\u001b]2;{Title}\u001b\\");
        writer.Write($"\u001b]30;{Title}\u001b\\");
        _lastTitle = Title;
    }

    public string Title { get; set; }

    private void SetCursorVisible(StreamWriter writer, bool isVisible)
    {
        // Uses DECTCEM
        if (isVisible)
        {
            TerminalDriver.HideCursor();
            writer.Write($"{Escape}?25h");
        }
        else
        {
            writer.Write($"{Escape}?25l");
            TerminalDriver.ShowCursor();
        }
    }
    
    private void SetCursorBlink(StreamWriter writer, bool isBlinking)
    {
        // Uses ATT160
        if (isBlinking)
            writer.Write($"{Escape}?12h");
        else
            writer.Write($"{Escape}?12l");
    }

    public void ResetStyle()
    {
        _cursor.Style = CellStyle.Normal;
    }

    public void ResetForeground() => _cursor.Foreground = 0;

    public void ResetBackground() => _cursor.Background = 0;

    public void SetForeground(byte red, byte green, byte blue) =>
        _cursor.Foreground = _foregroundPalette.GetForegroundIndex(red, green, blue);

    public void SetForeground(byte index) =>
        _cursor.Foreground = _foregroundPalette.GetForegroundIndex(index);

    public void SetForeground(AnsiColor color) =>
        _cursor.Foreground = _foregroundPalette.GetForegroundIndex(color);

    public void SetBackground(byte red, byte green, byte blue) =>
        _cursor.Background = _backgroundPalette.GetBackgroundIndex(red, green, blue);

    public void SetBackground(byte index) =>
        _cursor.Background = _backgroundPalette.GetBackgroundIndex(index);

    public void SetBackground(AnsiColor color) =>
        _cursor.Background = _backgroundPalette.GetBackgroundIndex(color);

    private void SetStyle(CellStyle style, bool enabled)
    {
        var wasBold = _cursor.Style.HasFlag(style);
        if (!wasBold && enabled)
            _cursor.Style |= style;
        else if (wasBold && !enabled)
            _cursor.Style &= ~style;
    }
    
    public void SetBold(bool isSet) => SetStyle(CellStyle.Bold, isSet);
    public void SetFaint(bool isSet) => SetStyle(CellStyle.Faint, isSet);
    public void SetItalic(bool isSet) => SetStyle(CellStyle.Italic, isSet);
    public void SetUnderline(bool isSet) => SetStyle(CellStyle.Underline, isSet);
    public void SetBlink(bool isSet) => SetStyle(CellStyle.Blink, isSet);
    public void SetInverse(bool isSet) => SetStyle(CellStyle.Inverse, isSet);
    public void SetStrikeout(bool isSet) => SetStyle(CellStyle.Strikeout, isSet);
    public void SetHidden(bool isSet) => SetStyle(CellStyle.Hidden, isSet);

    private (int column, int row) GetCursorPosition(int index) => (index % Columns, index / Columns);

    private int GetCursorIndex(int column, int row) =>
        Math.Clamp(column, 0, Columns - 1) + Math.Clamp(row, 0, Rows - 1) * Columns;

    private void Write(StreamWriter writer, int start, int end)
    {
        if (_cursorColumn == -1 || _cursorRow == -1)
        {
            // writer.Write($"{Escape}0m");
        }

        MoveCursor(writer, start);
        for (var index = start; index <= end; index++)
        {
            var cell = _nextFrame[index];
            _screenBuffer[index] = cell;
            UpdateStyle(writer, cell);

            writer.Write(cell.Character);
            _cursorColumn++;
            if (_cursorColumn >= Columns)
            {
                _cursorColumn = 0;
                _cursorRow++;
            }
        }
        if (DebugOutput) writer.Write("\r\n");
    }

    private void MoveCursor(StreamWriter writer, int index)
    {
        var (column, row) = GetCursorPosition(index);
        MoveCursor(writer, column, row);
    }

    private void MoveCursor(StreamWriter writer, int column, int row)
    {
        if (row == _cursorRow && column == _cursorColumn) return;

        if (_cursorColumn == -1 || _cursorRow == -1)
        {
            writer.Write($"{Escape}{row};{column}H"); // Move to start
            return;
        }

        if (row == _cursorRow)
        {
            var difference = column - _cursorColumn;
            if (difference > 0) // Move Right
                writer.Write($"{Escape}{difference}C");
            else
                writer.Write($"{Escape}{difference}D");
            Console.Write($"MOVE{difference}");
            return;
        }

        if (column == _cursorColumn)
        {
            var difference = row - _cursorRow;
            if (difference > 0) // Move Right
                writer.Write($"{Escape}{difference}A");
            else
                writer.Write($"{Escape}{difference}B");
            return;
        }

        writer.Write($"{Escape}{row};{column}H"); // Move to destination
    }

    private void UpdateStyle(StreamWriter writer, Cell cell)
    {
        _styles.Clear();

        if (cell.Style != _cursor.Style)
        {
            if (cell.Style.HasFlag(CellStyle.Bold) != _cursor.Style.HasFlag(CellStyle.Bold)
                || cell.Style.HasFlag(CellStyle.Faint) != _cursor.Style.HasFlag(CellStyle.Faint))
            {
                _styles.Add("22");
                if (cell.Style.HasFlag(CellStyle.Bold)) _styles.Add("1");
                if (cell.Style.HasFlag(CellStyle.Faint)) _styles.Add("2");
            }

            CheckFlag(CellStyle.Italic, "3");
            CheckFlag(CellStyle.Underline, "4");
            CheckFlag(CellStyle.Blink, "5");
            CheckFlag(CellStyle.Inverse, "7");
            CheckFlag(CellStyle.Hidden, "8");
            CheckFlag(CellStyle.Strikeout, "9");
        }

        if (cell.Foreground != _cursor.Foreground)
            _styles.AddRange(_foregroundPalette.GetCodes(cell.Foreground));
        
        if (cell.Background != _cursor.Background)
            _styles.AddRange(_backgroundPalette.GetCodes(cell.Background));

        if (_styles.Count == 0) return;

        writer.Write($"{Escape}{string.Join(";", _styles)}m");

        _cursor = cell;
        return;

        void CheckFlag(CellStyle style, string code)
        {
            if (cell.Style.HasFlag(style) != _cursor.Style.HasFlag(style))
                _styles.Add(cell.Style.HasFlag(style) ? code : $"2{code}");
        }
    }

    private void IdentifyChanges()
    {
        var length = _screenBuffer.Length;

        // Interpret the Cell arrays as int arrays for efficient comparison
        var oldScreen = MemoryMarshal.Cast<Cell, long>(_screenBuffer.AsSpan());
        var newScreen = MemoryMarshal.Cast<Cell, long>(_nextFrame.AsSpan());

        // Track all changes
        _changes.Clear();
        var currentStart = -1;
        var currentEnd = -1;

        for (var index = 0; index < length; index++)
        {
            if (oldScreen[index] == newScreen[index]) continue;

            if (currentStart == -1)
            {
                // Start a new change range
                currentStart = index;
                currentEnd = index;
            }
            else if (index <= currentEnd + 4)
            {
                // Extend the current range if within 4 cells
                currentEnd = index;
            }
            else
            {
                // Add the completed range and start a new one
                _changes.Add((currentStart, currentEnd));
                currentStart = index;
                currentEnd = index;
            }
        }

        // Add the final range if it exists
        if (currentStart != -1)
        {
            _changes.Add((currentStart, currentEnd));
        }
    }
}