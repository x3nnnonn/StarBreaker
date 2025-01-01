using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace StarBreaker.Common;

public static class StreamExtensions
{
    public static T Read<T>(this Stream stream) where T : unmanaged
    {
        var size = Unsafe.SizeOf<T>();

        if (size > 256)
            throw new Exception("Size is too large");

        Span<byte> span = stackalloc byte[size];

        if (stream.Read(span) != size)
            throw new Exception("Failed to read from stream");

        return MemoryMarshal.Read<T>(span);
    }

    public static void CopyAmountTo(this Stream source, Stream destination, int byteCount)
    {
        var rent = ArrayPool<byte>.Shared.Rent(byteCount);
        var buffer = rent.AsSpan(0, byteCount);
        try
        {
            source.ReadExactly(buffer);
            destination.Write(buffer);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rent);
        }
    }
}