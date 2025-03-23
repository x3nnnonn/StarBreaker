using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace StarBreaker.Common;

public static class Crc32c
{
    public static uint FromSpan(ReadOnlySpan<byte> data, uint seed = 0u)
    {
        var acc = ~seed;
        var remaining = data.Length;

        if (remaining >= sizeof(ulong))
        {
            var span = MemoryMarshal.Cast<byte, ulong>(data);

            foreach (var u in span)
                acc = BitOperations.Crc32C(acc, u);

            remaining -= span.Length * sizeof(ulong);
        }

        if (remaining >= sizeof(uint))
        {
            var span = MemoryMarshal.Cast<byte, uint>(data[^remaining..]);

            foreach (var u in span)
                acc = BitOperations.Crc32C(acc, u);

            remaining -= span.Length * sizeof(uint);
        }

        if (remaining > 0)
        {
            foreach (var t in data[^remaining..])
                acc = BitOperations.Crc32C(acc, t);
        }

        return ~acc;
    }

    public static uint FromSpan(Span<byte> data) => FromSpan((ReadOnlySpan<byte>)data);

    public static uint FromString(string data)
    {
        var length = Encoding.UTF8.GetByteCount(data);
        Span<byte> bytes = stackalloc byte[length];
        Encoding.UTF8.GetBytes(data, bytes);
        return FromSpan(bytes);
    }
}