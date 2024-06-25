using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace StarBreaker.Forge;

public ref struct SpanReader
{
    private readonly ReadOnlySpan<byte> _span;
    private int _position;
    
    // ReSharper disable ConvertToAutoPropertyWhenPossible
    public int Position => _position;
    
    public SpanReader(ReadOnlySpan<byte> span, int offset)
    {
        _span = span;
        _position = offset;
    }

    public T Read<T>() where T : unmanaged
    {
        var value = MemoryMarshal.Read<T>(_span[_position..]);
        _position += Unsafe.SizeOf<T>();
        return value;
    }
        
    public ReadOnlySpan<byte> ReadSpan(int count)
    {
        var span = _span.Slice(_position, count);
        _position += count;
        return span;
    }
    
    public T[] ReadArray<T>(int count) where T : unmanaged
    {
        var array = MemoryMarshal.Cast<byte, T>(_span.Slice(_position, count * Unsafe.SizeOf<T>())).ToArray();
        _position += count * Unsafe.SizeOf<T>();
        return array;
    }
}