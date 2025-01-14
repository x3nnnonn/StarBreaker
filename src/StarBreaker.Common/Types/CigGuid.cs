using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

namespace StarBreaker.Common;

[DebuggerDisplay("{ToString()}")]
public readonly record struct CigGuid
{
    public static readonly CigGuid Empty = default;
    private static readonly char[] _map = ['0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f'];

    internal readonly byte _0;
    internal readonly byte _1;
    internal readonly byte _2;
    internal readonly byte _3;
    internal readonly byte _4;
    internal readonly byte _5;
    internal readonly byte _6;
    internal readonly byte _7;
    internal readonly byte _8;
    internal readonly byte _9;
    internal readonly byte _a;
    internal readonly byte _b;
    internal readonly byte _c;
    internal readonly byte _d;
    internal readonly byte _e;
    internal readonly byte _f;

    public CigGuid(string input)
    {
        if (input.Length != 36)
        {
            throw new FormatException("Input string was not in a correct format.");
        }

        var span = input.AsSpan();

        _7 = ParseHexDigit(span, 00);
        _6 = ParseHexDigit(span, 02);
        _5 = ParseHexDigit(span, 04);
        _4 = ParseHexDigit(span, 06);
        // skip the hyphen
        _3 = ParseHexDigit(span, 09);
        _2 = ParseHexDigit(span, 11);
        // skip the hyphen
        _1 = ParseHexDigit(span, 14);
        _0 = ParseHexDigit(span, 16);
        // skip the hyphen
        _f = ParseHexDigit(span, 19);
        _e = ParseHexDigit(span, 21);
        // skip the hyphen
        _d = ParseHexDigit(span, 24);
        _c = ParseHexDigit(span, 26);
        _b = ParseHexDigit(span, 28);
        _a = ParseHexDigit(span, 30);
        _9 = ParseHexDigit(span, 32);
        _8 = ParseHexDigit(span, 34);

        return;

        static byte ParseHexDigit(ReadOnlySpan<char> input, int offset)
        {
            if (!byte.TryParse(input.Slice(offset, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var result))
                throw new FormatException("Input string was not in a correct format.");

            return result;
        }
    }

    public override string ToString()
    {
        var str = new StringWriter(new StringBuilder(36));
        WriteInto(str);
        return str.ToString();
    }

    public void WriteInto(TextWriter writer)
    {
        Span<char> buffer = stackalloc char[36];

        WriteHexDigit(buffer, _7, 00);
        WriteHexDigit(buffer, _6, 02);
        WriteHexDigit(buffer, _5, 04);
        WriteHexDigit(buffer, _4, 06);
        buffer[08] = '-';
        WriteHexDigit(buffer, _3, 09);
        WriteHexDigit(buffer, _2, 11);
        buffer[13] = '-';
        WriteHexDigit(buffer, _1, 14);
        WriteHexDigit(buffer, _0, 16);
        buffer[18] = '-';
        WriteHexDigit(buffer, _f, 19);
        WriteHexDigit(buffer, _e, 21);
        buffer[23] = '-';
        WriteHexDigit(buffer, _d, 24);
        WriteHexDigit(buffer, _c, 26);
        WriteHexDigit(buffer, _b, 28);
        WriteHexDigit(buffer, _a, 30);
        WriteHexDigit(buffer, _9, 32);
        WriteHexDigit(buffer, _8, 34);

        writer.Write(buffer);

        return;

        static void WriteHexDigit(Span<char> buffer, byte value, int offset)
        {
            buffer[offset] = _map[value >> 4];
            buffer[offset + 1] = _map[value & 15];
        }
    }
}