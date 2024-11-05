namespace DotNetFoss.Screens;

internal class AnsiPalette
{
    public const short PaletteSize = 1024;
    private readonly Func<int[]> _countUsages;
    private readonly Dictionary<int, short> _lookup = new(PaletteSize);
    private readonly string[]?[] _codes = new string[PaletteSize][];
    private int _nextIndex = 1;

    public AnsiPalette(Func<int[]> countUsages, string reset)
    {
        _countUsages = countUsages;
        _codes[0] = [reset];
    }

    public void Clear()
    {
        _nextIndex = 1;
        _lookup.Clear();
        for (var i = 1; i < PaletteSize; i++)
            _codes[i] = null;
    }

    public string[] GetCodes(short index) => _codes[index] ?? [];

    public short GetForegroundIndex(byte red, byte green, byte blue)
    {
        var key = 2 + (red << 8) + (green << 16) + (blue << 24);
        return TryGetIndex(key, out var index)
            ? index
            : AddIndex(key, ["38", "2", red.ToString(), green.ToString(), blue.ToString()]);
    }

    public short GetBackgroundIndex(byte red, byte green, byte blue)
    {
        var key = 2 + (red << 8) + (green << 16) + (blue << 24);
        return TryGetIndex(key, out var index)
            ? index
            : AddIndex(key, ["48", "2", red.ToString(), green.ToString(), blue.ToString()]);
    }

    public short GetForegroundIndex(byte color)
    {
        var key = 2 + (color << 8);
        return TryGetIndex(key, out var index)
            ? index
            : AddIndex(key, ["38", "5", color.ToString()]);
    }

    public short GetBackgroundIndex(byte color)
    {
        var key = 2 + (color << 8);
        return TryGetIndex(key, out var index)
            ? index
            : AddIndex(key, ["48", "5", color.ToString()]);
    }

    public short GetForegroundIndex(AnsiColor color)
    {
        return color switch
        {
            AnsiColor.Black => GetIndex(30, ["30"]),
            AnsiColor.Red => GetIndex(31, ["31"]),
            AnsiColor.Green => GetIndex(32, ["32"]),
            AnsiColor.Yellow => GetIndex(33, ["33"]),
            AnsiColor.Blue => GetIndex(34, ["34"]),
            AnsiColor.Magenta => GetIndex(35, ["35"]),
            AnsiColor.Cyan => GetIndex(36, ["36"]),
            AnsiColor.White => GetIndex(37, ["37"]),
            _ => throw new ArgumentOutOfRangeException(nameof(color), color, null)
        };
    }

    public short GetBackgroundIndex(AnsiColor color)
    {
        return color switch
        {
            AnsiColor.Black => GetIndex(40, ["40"]),
            AnsiColor.Red => GetIndex(41, ["41"]),
            AnsiColor.Green => GetIndex(42, ["42"]),
            AnsiColor.Yellow => GetIndex(43, ["43"]),
            AnsiColor.Blue => GetIndex(44, ["44"]),
            AnsiColor.Magenta => GetIndex(45, ["45"]),
            AnsiColor.Cyan => GetIndex(46, ["46"]),
            AnsiColor.White => GetIndex(47, ["47"]),
            _ => throw new ArgumentOutOfRangeException(nameof(color), color, null)
        };
    }

    // NOTE: Use this paired with AddIndex() for constructed arrays
    private bool TryGetIndex(int key, out short index)
    {
        if (key == 0)
        {
            index = 0;
            return true;
        }

        return _lookup.TryGetValue(key, out index);
    }

    // NOTE: Use this only with const arrays
    private short GetIndex(int key, string[] codes)
    {
        return TryGetIndex(key, out var index)
            ? index
            : AddIndex(key, codes);
    }

    private short AddIndex(int key, string[] codes)
    {
        // Add color to table
        if (_nextIndex < PaletteSize && _codes[_nextIndex] is null)
        {
            _codes[_nextIndex] = codes;
            _lookup[key] = (short)_nextIndex;
            return (short)_nextIndex++;
        }

        // Replace color in table
        var index = Array.IndexOf(_codes, null);
        if (index < 0) index = Purge();
        _codes[index] = codes;
        _lookup[key] = (short)index;
        return (short)index;
    }

    private int Purge()
    {
        var usages = _countUsages.Invoke();
        var minUsages = 1;
        var minUsedIndex = usages[1];
        var lastClearedIndex = 0;

        // Purge all unused
        for (var i = 1; i < PaletteSize; i++)
        {
            // Track the least used index in case we need it
            if (usages[i] != 0)
            {
                if (usages[i] < minUsages)
                {
                    minUsages = usages[i];
                    minUsedIndex = i;
                }

                continue;
            }

            // Purse the entry
            _lookup.Remove(i);
            _codes[i] = null;
            lastClearedIndex = i;
        }

        // Return first unused
        // We return the last one so the next Array.IndexOf can be faster
        if (lastClearedIndex > 0)
            return lastClearedIndex;

        // If all used, purge the least used
        return minUsedIndex;
    }
}