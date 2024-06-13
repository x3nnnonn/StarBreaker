using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace StarBreaker.Forge;

public class ArrayReader(byte[] bytes, int initialPosition = 0)
{
    private int _position = initialPosition;
    private readonly byte[] _bytes = bytes;

    // ReSharper disable ConvertToAutoPropertyWhenPossible
    public int Position => _position;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Read<T>() where T : unmanaged
    {
        var value = MemoryMarshal.Read<T>(_bytes.AsSpan(_position));
        _position += Unsafe.SizeOf<T>();
        return value;
    }
    
    public ReadOnlySpan<byte> ReadSpan(int count)
    {
        var span = _bytes.AsSpan(_position, count);
        _position += count;
        return span;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlyMemory<T> GetMemory<T>(int count) where T : unmanaged
    {
        var byteLength = count * Unsafe.SizeOf<T>();
        var memory = new CastMemoryManager<byte, T>(_bytes.AsMemory(_position, byteLength)).Memory;
        _position += byteLength;
        return memory;
    }
    
    //https://stackoverflow.com/questions/54511330/how-can-i-cast-memoryt-to-another
    private sealed class CastMemoryManager<TFrom, TTo> : MemoryManager<TTo>
        where TFrom : unmanaged
        where TTo : unmanaged
    {
        private readonly Memory<TFrom> _from;
        public CastMemoryManager(Memory<TFrom> from) => _from = from;
        public override Span<TTo> GetSpan() => MemoryMarshal.Cast<TFrom, TTo>(_from.Span);
        protected override void Dispose(bool disposing) { }
        public override MemoryHandle Pin(int elementIndex = 0) => throw new NotSupportedException();
        public override void Unpin() => throw new NotSupportedException();
    }
}