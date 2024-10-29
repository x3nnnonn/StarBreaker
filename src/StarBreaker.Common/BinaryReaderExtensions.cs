using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace StarBreaker.Common;

public static class BinaryReaderExtensions
{
    public static T Read<T>(this BinaryReader br) where T : unmanaged
    {
        var size = Unsafe.SizeOf<T>();
        
        if (size > 256)
            throw new Exception("Size is too large");
        
        Span<byte> span = stackalloc byte[size];
        
        br.BaseStream.ReadExactly(span);
        
        return MemoryMarshal.Read<T>(span);
    }
    
    public static T[] ReadArray<T>(this BinaryReader reader, int count) where T : unmanaged
    {
        var items = new T[count];
        
        var bytes = MemoryMarshal.Cast<T, byte>(items);

        reader.BaseStream.ReadExactly(bytes);
        
        return items;
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

    public static string ReadStringOfLength(this BinaryReader br, int length)
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
}