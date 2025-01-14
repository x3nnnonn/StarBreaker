﻿using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace StarBreaker.Common;

public ref struct SpanReader
{
    private readonly ReadOnlySpan<byte> _span;
    private int _position;

    public SpanReader(ReadOnlySpan<byte> span, int position = 0)
    {
        if (!BitConverter.IsLittleEndian)
            throw new Exception("todo: big endian support? probably not relevant");

        _span = span;
        _position = position;
    }

    public ReadOnlySpan<byte> PeekBytes(int count)
    {
#if DEBUG
        if (_position + count > _span.Length)
        {
            Debugger.Break();            
            throw new Exception("Reading past the end of the span");
        }
#endif

        return _span.Slice(_position, count);
    }

    public ReadOnlySpan<byte> ReadBytes(int count)
    {
#if DEBUG
        if (_position + count > _span.Length)
        {
            Debugger.Break();            
            throw new Exception("Reading past the end of the span");
        }

#endif

        var span = _span.Slice(_position, count);
        _position += count;
        return span;
    }

    public void Expect<T>(T value) where T : unmanaged
    {
        var actual = Read<T>();
        if (!actual.Equals(value))
            throw new Exception($"Expected {value}, got {actual}");
    }

    public void ExpectAny<T>(scoped ReadOnlySpan<T> values) where T : unmanaged, IEquatable<T>
    {
        var actual = Read<T>();
        if (!values.Contains(actual))
            throw new Exception($"Expected {values.ToString()}, got {actual}");
    }

    public void ExpectAny<T>(scoped Span<T> values) where T : unmanaged, IEquatable<T>
    {
        var actual = Read<T>();
        if (!values.Contains(actual))
            throw new Exception($"Expected {values.ToString()}, got {actual}");
    }

    public void ExpectKey(string key)
    {
        var uintKey = Crc32c.FromString(key);
        Expect(uintKey);
    }

    public void ExpectAnyKey(scoped Span<string> keys)
    {
        Span<uint> uintKeys = stackalloc uint[keys.Length];

        for (var i = 0; i < keys.Length; i++)
            uintKeys[i] = Crc32c.FromString(keys[i]);

        ExpectAny(uintKeys);
    }

    public int Length => _span.Length;

    // ReSharper disable once ConvertToAutoPropertyWithPrivateSetter
    public int Position => _position;

    public int Remaining => _span.Length - _position;

    public uint NextKey => Peek<uint>();

    public ReadOnlySpan<byte> RemainingBytes => _span[_position..];

    public void Seek(int offset) => _position = offset;

    public void Advance(int count) => _position += count;

    public ReadOnlySpan<T> ReadSpan<T>(int count) where T : unmanaged => MemoryMarshal.Cast<byte, T>(ReadBytes(count * Unsafe.SizeOf<T>()));

    public T Read<T>() where T : unmanaged => MemoryMarshal.Read<T>(ReadBytes(Unsafe.SizeOf<T>()));

    public bool ReadBoolean() => ReadByte() != 0;

    public byte ReadByte() => _span[_position++];

    public sbyte ReadSByte() => (sbyte)ReadByte();

    public Half ReadHalf(bool littleEndian = true) => littleEndian
        ? BinaryPrimitives.ReadHalfLittleEndian(ReadBytes(Unsafe.SizeOf<Half>()))
        : BinaryPrimitives.ReadHalfBigEndian(ReadBytes(Unsafe.SizeOf<Half>()));

    public float ReadSingle(bool littleEndian = true) => littleEndian
        ? BinaryPrimitives.ReadSingleLittleEndian(ReadBytes(sizeof(float)))
        : BinaryPrimitives.ReadSingleBigEndian(ReadBytes(sizeof(float)));

    public double ReadDouble(bool littleEndian = true) => littleEndian
        ? BinaryPrimitives.ReadDoubleLittleEndian(ReadBytes(sizeof(double)))
        : BinaryPrimitives.ReadDoubleBigEndian(ReadBytes(sizeof(double)));

    public short ReadInt16(bool littleEndian = true) => littleEndian
        ? BinaryPrimitives.ReadInt16LittleEndian(ReadBytes(sizeof(short)))
        : BinaryPrimitives.ReadInt16BigEndian(ReadBytes(sizeof(short)));

    public int ReadInt32(bool littleEndian = true) => littleEndian
        ? BinaryPrimitives.ReadInt32LittleEndian(ReadBytes(sizeof(int)))
        : BinaryPrimitives.ReadInt32BigEndian(ReadBytes(sizeof(int)));

    public long ReadInt64(bool littleEndian = true) => littleEndian
        ? BinaryPrimitives.ReadInt64LittleEndian(ReadBytes(sizeof(long)))
        : BinaryPrimitives.ReadInt64BigEndian(ReadBytes(sizeof(long)));

    public ushort ReadUInt16(bool littleEndian = true) => littleEndian
        ? BinaryPrimitives.ReadUInt16LittleEndian(ReadBytes(sizeof(ushort)))
        : BinaryPrimitives.ReadUInt16BigEndian(ReadBytes(sizeof(ushort)));

    public uint ReadUInt32(bool littleEndian = true) => littleEndian
        ? BinaryPrimitives.ReadUInt32LittleEndian(ReadBytes(sizeof(uint)))
        : BinaryPrimitives.ReadUInt32BigEndian(ReadBytes(sizeof(uint)));

    public ulong ReadUInt64(bool littleEndian = true) => littleEndian
        ? BinaryPrimitives.ReadUInt64LittleEndian(ReadBytes(sizeof(ulong)))
        : BinaryPrimitives.ReadUInt64BigEndian(ReadBytes(sizeof(ulong)));

    public string ReadStringOfLength(ushort length)
    {
        if (length == 0)
            return string.Empty;

        if (length >= 0xffff)
            throw new Exception("Size is too large");

        return Encoding.ASCII.GetString(ReadBytes(length));
    }

    public string ReadStringInsideArray(int length)
    {
        var bytes = ReadBytes(length);
        var strLength = bytes.IndexOf((byte)0);
        return Encoding.ASCII.GetString(bytes[..strLength]);
    }

    /// <summary>
    ///     Reads a value from the span without advancing the position.
    /// </summary>
    public T Peek<T>() where T : unmanaged
    {
        if (typeof(T) == typeof(bool))
            throw new InvalidOperationException("Read an int and compare it to 0 instead");

        return MemoryMarshal.Read<T>(PeekBytes(Unsafe.SizeOf<T>()));
    }
}