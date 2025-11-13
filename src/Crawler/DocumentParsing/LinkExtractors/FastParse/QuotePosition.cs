using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BrokenLinkChecker.DocumentParsing.ModularLinkExtraction.FastParse;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct QuotePosition
{
    // Adjusted bit layout:
    // - 20 bits for start (allows positions up to 1,048,575)
    // - 11 bits for length (allows lengths up to 2,047)
    // - 1 bit for valid flag
    private const uint VALID_FLAG = 1u << 31;
    private const int LENGTH_SHIFT = 20;
    private const uint START_MASK = 0xFFFFF;        // 20 bits
    private const uint LENGTH_MASK = 0x7FF;         // 11 bits
    
    private readonly uint _data;

    public int Start
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (int)(_data & START_MASK);
    }

    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (int)((_data >> LENGTH_SHIFT) & LENGTH_MASK);
    }

    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (_data & VALID_FLAG) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public QuotePosition(int start, int length, bool isValid)
    {
        // Validate inputs
        if (start < 0 || start > START_MASK || length < 0 || length > LENGTH_MASK)
        {
            _data = 0;
            return;
        }

        _data = ((uint)start & START_MASK) |
                (((uint)length & LENGTH_MASK) << LENGTH_SHIFT) |
                (isValid ? VALID_FLAG : 0);
    }

    public override string ToString() => 
        $"QuotePosition(Start={Start}, Length={Length}, IsValid={IsValid})";
}