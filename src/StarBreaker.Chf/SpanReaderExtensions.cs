using System.Runtime.CompilerServices;
using StarBreaker.Common;

namespace StarBreaker.Chf;

public static class SpanReaderExtensions
{
    public static T ReadKeyedValue<T>(this ref SpanReader reader, uint key) where T : unmanaged
    {
        if (Unsafe.SizeOf<T>() != 4)
            throw new Exception("Only 4-byte values are supported");
        
        reader.Expect(key);
        var data = reader.Read<T>();
        reader.Expect(0);
        
        return data;
    }
}