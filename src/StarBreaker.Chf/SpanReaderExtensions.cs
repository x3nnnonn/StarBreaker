using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.Arm;
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
    
    public static T ReadKeyedValue<T>(this ref SpanReader reader, string key) where T : unmanaged
    {
        var keyUint = Crc32c.FromString(key);
        
        if (Unsafe.SizeOf<T>() != 4)
            throw new Exception("Only 4-byte values are supported");
        
        reader.Expect(keyUint);
        var data = reader.Read<T>();
        reader.Expect(0);
        
        return data;
    }
}