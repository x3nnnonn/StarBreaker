using System.Buffers;
using System.Collections;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace StarBreaker.Common;

public static class Crc32c
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint FromSpan(ReadOnlySpan<byte> data)
    {
        var acc = 0xFFFFFFFFu;
        
        foreach (ref readonly var t in data)
        {
            acc = BitOperations.Crc32C(acc, t);
        }

        return ~acc;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint FromSpan(Span<byte> data) => FromSpan((ReadOnlySpan<byte>)data);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint FromString(string data)
    {
        var length = Encoding.UTF8.GetByteCount(data);
        Span<byte> bytes = stackalloc byte[length];
        Encoding.UTF8.GetBytes(data, bytes);
        return FromSpan(bytes);
    }
}