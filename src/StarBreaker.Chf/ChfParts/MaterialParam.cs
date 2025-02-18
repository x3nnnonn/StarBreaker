using System.Runtime.CompilerServices;
using StarBreaker.Common;

namespace StarBreaker.Chf;

public class MaterialParam<T> where T : unmanaged
{
    public required NameHash Name { get; init; }
    public required T Value { get; init; }

    static MaterialParam()
    {
        if (Unsafe.SizeOf<T>() != 4)
            throw new Exception("Invalid size");
    }
    
    public static MaterialParam<T> Read(ref SpanReader reader)
    {
        var key = reader.Read<NameHash>();
        var value = reader.Read<T>();
        reader.Expect<uint>(0);
        
        return new MaterialParam<T>
        {
            Name = key,
            Value = value
        };
    }
}