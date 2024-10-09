using System.Numerics;
using System.Runtime.InteropServices;
using StarBreaker.Common;
using ZstdSharp;

namespace StarBreaker.Chf;

public class ChfFile(byte[] data, bool isModded)
{
    public const int Size = 4096;
    private static ushort CigMagic => 0x4242;
    private static ReadOnlySpan<byte> MyMagic => "diogotr7"u8;
    
    public byte[] Data { get; } = data;
    public bool Modded { get; } = isModded;

    public static ChfFile FromBin(string file, bool isModded = true)
    {
        if (!file.EndsWith(".bin"))
            throw new ArgumentException("File must be a .bin file");
        
        var data = File.ReadAllBytes(file);
        
        return new ChfFile(data, isModded);
    }

    public static ChfFile FromChf(string file)
    {
        if (!file.EndsWith(".chf"))
            throw new ArgumentException("File must be a .chf file");
        
        var fileBytes = File.ReadAllBytes(file);
        var span = fileBytes.AsSpan();
        
        if (fileBytes.Length != Size)
            throw new ArgumentException("Invalid data length");

        var reader = new SpanReader(span);
        
        reader.Expect(CigMagic);
        
        //these 2 bytes used to be 0x00, part of the magic, not anymore. investigate.
        //Could be version number but that seems unlikely.
        // ReSharper disable once UnusedVariable
        var _unknown = reader.ReadBytes(2);
        Console.WriteLine($"Unknown Magic bytes: {BitConverter.ToString(_unknown.ToArray())} in {file}");
        
        var expectedCrc = reader.Read<uint>();
        var compressedSize = reader.Read<uint>();
        var uncompressedSize = reader.Read<uint>();
        
        var actualCrc = Crc32C(reader.RemainingBytes);
        if (actualCrc != expectedCrc)
            throw new Exception("CRC32 does not match");
        
        var uncompressed = new byte[uncompressedSize];
        
        using var zstd = new Decompressor();
        var written = zstd.Unwrap(reader.ReadBytes((int)compressedSize), uncompressed);


        if (written != uncompressedSize)
            throw new Exception("Decompressed size does not match expected size");
                
        //expect zeroes until the last 8 bytes
        reader.ExpectAll<byte>(0, reader.Remaining - 8);
        var isModded = IsModded(reader.ReadBytes(8));
        
        return new ChfFile(uncompressed, isModded);
    }
    
    public async Task WriteToChfFileAsync(string file)
    {
        if (!file.EndsWith(".chf"))
            throw new ArgumentException("File must be a .chf file");
        
        var compressed = GetChfBuffer();
        
        await File.WriteAllBytesAsync(file, compressed);
    }
    
    public async Task WriteToBinFileAsync(string file)
    {
        if (!file.EndsWith(".bin"))
            throw new ArgumentException("File must be a .bin file");
        
        await File.WriteAllBytesAsync(file, Data);
    }
    
    private byte[] GetChfBuffer()
    {
        using var zstd = new Compressor();
        
        var output = new byte[Size];
        var writtenBytes = zstd.Wrap(Data, output, 16);
        var span = output.AsSpan();
        
        MemoryMarshal.Write(span[0..4], CigMagic);
        MemoryMarshal.Write(span[4..8], 0);//placeholder crc32
        MemoryMarshal.Write(span[8..12], (uint)writtenBytes);
        MemoryMarshal.Write(span[12..16], (uint)Data.Length);
        
        //Insert our magic at the end so we can tell if it's a modded character.
        if (Modded)
            MyMagic.CopyTo(span[(Size - MyMagic.Length)..]);
        
        var crc = Crc32C(span[16..]);
        
        BitConverter.TryWriteBytes(span[4..8], crc);
        
        return output;
    }
    
    private static bool IsModded(ReadOnlySpan<byte> data)
    {
        ReadOnlySpan<byte> zeroes = [0, 0, 0, 0, 0, 0, 0, 0];
        return data.EndsWith(MyMagic) || data.EndsWith(zeroes);
    }
    
    private static uint Crc32C(ReadOnlySpan<byte> data)
    {
        var acc = 0xFFFFFFFFu;
        foreach (ref readonly var t in data)
        {
            acc = BitOperations.Crc32C(acc, t);
        }
        return ~acc;
    }
}