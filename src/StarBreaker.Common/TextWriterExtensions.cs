using System.Runtime.CompilerServices;
using System.Text;

namespace StarBreaker.Common;

public static class TextWriterExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteString(this TextWriter writer, ReadOnlySpan<byte> data, int offset)
    {
        var relevantData = data[offset..];
        var length = relevantData.IndexOf((byte)'\0');

        if (length == 0)
            return;

        Span<char> span = stackalloc char[length];
        Encoding.ASCII.GetChars(relevantData[..length], span);
        writer.Write(span);
    }
}