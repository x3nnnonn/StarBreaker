using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace StarBreaker.Common;

public static class BinaryReaderExtensions
{
    public static T[] ReadArray<T>(this BinaryReader reader, int count) where T : unmanaged
    {
        var bytes = reader.ReadBytes(count * Unsafe.SizeOf<T>());
        var array = MemoryMarshal.Cast<byte, T>(bytes).ToArray();
        return array;
    }
    
    public static long Locate(this BinaryReader br, ReadOnlySpan<byte> magic, long bytesFromEnd = 0)
    {
        const int chunkSize = 8192;

        var rent = ArrayPool<byte>.Shared.Rent(chunkSize);
        var search = rent.AsSpan();
        br.BaseStream.Seek(-bytesFromEnd, SeekOrigin.End);

        try
        {
            var location = -1;
            
            while (location == -1)
            {
                br.BaseStream.Seek(-rent.Length, SeekOrigin.Current);

                if (br.Read(rent, 0, rent.Length) != rent.Length)
                    throw new Exception("Failed to read end of central directory record");
        
                location = search.LastIndexOf(magic);
            }

            return br.BaseStream.Position + location - rent.Length;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rent);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T ReadStruct<T>(this BinaryReader br) where T : unmanaged
    {
        var size = Unsafe.SizeOf<T>();
        
        if (size > 256)
            throw new Exception("Size is too large");
        
        Span<byte> span = stackalloc byte[size];
        
        if (br.Read(span) != size)
            throw new Exception("Failed to read from stream");
        
        return MemoryMarshal.Read<T>(span);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ReadStringCustom(this BinaryReader br, int length)
    {
        if (length >= 0xffff)
            throw new Exception("Size is too large");
        
        if (length == 0)
            return string.Empty;
        
        Span<byte> span = stackalloc byte[length];
        
        if (br.Read(span) != length)
            throw new Exception("Failed to read from stream");
        
        return Encoding.ASCII.GetString(span);
    }
    
    public static void Expect<T>(this BinaryReader br, T value) where T : unmanaged
    {
        var actual = br.ReadStruct<T>();
        if (!EqualityComparer<T>.Default.Equals(actual, value))
            throw new Exception($"Expected {value}, got {actual}");
    }
}