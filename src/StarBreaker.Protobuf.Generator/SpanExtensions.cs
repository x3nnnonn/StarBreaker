namespace StarBreaker.Protobuf.Generator;

public static class SpanExtensions
{
    public static ulong DecodeVarInt(this Span<byte> span, int pos, out ulong outPos)
    {
        var result = 0;
        var shift = 0;
        while (true)
        {
            var b = span[pos];
            result |= (b & 0x7f) << shift;
            pos += 1;
            if ((b & 0x80) == 0)
            {
                outPos = (ulong)pos;
                return (ulong)result;
            }
            shift += 7;
            if (shift >= 64)
                throw new Exception("Too many bytes when decoding varint.");
        }
    }
}