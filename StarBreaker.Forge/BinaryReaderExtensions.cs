using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace StarBreaker.Forge;

public static class BinaryReaderExtensions
{
    public static T[] ReadArray<T>(this BinaryReader reader, int count) where T : unmanaged
    {
        var bytes = reader.ReadBytes(count * Unsafe.SizeOf<T>());
        var array = MemoryMarshal.Cast<byte, T>(bytes).ToArray();
        return array;
    }
}