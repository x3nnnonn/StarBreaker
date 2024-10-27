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
    
    public static void ReadToEnd(this Stream stream, Span<byte> buffer)
    {
        var read = 0;
        while (read < buffer.Length)
        {
            var n = stream.Read(buffer[read..]);
            if (n == 0)
                throw new Exception("Failed to read from stream");
            read += n;
        }
    }
}