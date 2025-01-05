using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

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

    public static T[] ReadArray<T>(this Stream stream, int count) where T : unmanaged
    {
        var items = new T[count];

        var bytes = MemoryMarshal.Cast<T, byte>(items);

        stream.ReadExactly(bytes);

        return items;
    }


    public static void Write<T>(this Stream stream, T value) where T : unmanaged
    {
        var size = Unsafe.SizeOf<T>();

        if (size > 256)
            throw new Exception("Size is too large");

        Span<byte> span = stackalloc byte[size];

        MemoryMarshal.Write(span, in value);

        stream.Write(span);
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

    public static long Locate(this Stream stream, ReadOnlySpan<byte> magic, long bytesFromEnd = 0)
    {
        const int chunkSize = 4096;

        var rent = ArrayPool<byte>.Shared.Rent(chunkSize);
        var search = rent.AsSpan();
        stream.Seek(-bytesFromEnd, SeekOrigin.End);

        try
        {
            var location = -1;

            while (location == -1)
            {
                // seek to the left by chunkSize + magic.Length.
                // this is to ensure we don't miss the magic bytes that are split between chunks
                stream.Seek((rent.Length + magic.Length) * -1, SeekOrigin.Current);

                if (stream.Read(rent, 0, rent.Length) != rent.Length)
                    throw new Exception("Failed to read end of central directory record");

                location = search.LastIndexOf(magic);
            }

            return stream.Position + location - rent.Length;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rent);
        }
    }

    public static string ReadStringOfLength(this Stream stream, int length)
    {
        if (length >= 0xffff)
            throw new Exception("Size is too large");

        if (length == 0)
            return string.Empty;

        Span<byte> span = stackalloc byte[length];

        if (stream.Read(span) != length)
            throw new Exception("Failed to read from stream");

        return Encoding.ASCII.GetString(span);
    }
}