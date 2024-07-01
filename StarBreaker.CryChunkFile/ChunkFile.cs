using System.Diagnostics;
using StarBreaker.Common;

namespace StarBreaker.CryChunkFile;

public sealed class ChunkFile
{
    private readonly byte[] _data;
    private const uint CrChMagic = 0x68437243; //"CrCh"u8
    private const uint IvoMagic = 0x6F766923;  //"#ivo"u8

    private ChunkFile(byte[] data)
    {
        _data = data;
    }

    public static bool TryOpen(byte[] data, out ChunkFile? chunkFile)
    {
        if (data.Length < 4)
        {
            chunkFile = null;
            return false;
        }

        var magic = BitConverter.ToUInt32(data, 0);
        if (magic != CrChMagic && magic != IvoMagic)
        {
            chunkFile = null;
            return false;
        }

        chunkFile = new ChunkFile(data);
        return true;
    }

    public void WriteXmlTo(TextWriter writer)
    {
        ReadOnlySpan<byte> span = _data;
        var reader = new SpanReader(span, 0);
        var signature = reader.ReadUInt32();
        
        if (signature == CrChMagic)
            WriteChCfTo(writer, ref reader);
        else if (signature == IvoMagic)
            WriteIvoTo(writer, ref reader);
        else
            throw new Exception("Invalid signature");
    }

    private void WriteChCfTo(TextWriter writer, ref SpanReader reader)
    {
        var version = reader.ReadUInt32();
        if (version != 0x746)//technically there are other versions but the latest SC only has this one.
            throw new Exception("Invalid version");
    }
    
    private void WriteIvoTo(TextWriter writer, ref SpanReader reader)
    {
        var version = reader.ReadUInt32();
        if(version != 0x900)
            throw new Exception("Invalid version");
        
        var chunkCount = reader.ReadUInt32();
        var chunkTableOffset = reader.ReadUInt32();
        Debug.Assert(chunkTableOffset == reader.Position);
        var headers = reader.ReadSpan<ChunkHeader>((int)chunkCount);
        Span<int> lengths = stackalloc int[headers.Length];
        GetChunkLengths(headers, lengths);
        
        var chunks = new IChunk[chunkCount];
        for (var i = 0; i < headers.Length; i++)
        {
            ref readonly var header = ref headers[i];
            //Debug.Assert(header.Offset == reader.GetPosition());
            reader.Seek((int)header.Offset);

            if (Enum.IsDefined(typeof(ChunkType), header.ChunkType) == false)
            {
                Console.WriteLine($"Unknown chunk type: {header.ChunkType} | {header.ChunkType:X}");
                continue;
            }

            chunks[i] = ReadChunk(ref reader, header.ChunkType);
        }
        
        Console.WriteLine("Done");
    }

    private void GetChunkLengths(ReadOnlySpan<ChunkHeader> headers, Span<int> lengths)
    {
        for (var i = 0; i < headers.Length; i++)
        {
            ref readonly var header = ref headers[i];
            if (i == headers.Length - 1)
                lengths[i] = (_data.Length - (int)header.Offset);
            else
                lengths[i] = (int)(headers[i + 1].Offset - header.Offset);
        }
    }

    private static IChunk ReadChunk(ref SpanReader reader, ChunkType chunkType)
    {
        return chunkType switch
        {
            //TODO: fill in
            ChunkType.Any => CompiledBoneChunk.Read(ref reader),
            ChunkType.CompiledBonesIvo320 => CompiledBoneChunk.Read(ref reader),
            ChunkType.MtlNameIvo320 => MaterialNameChunk.Read(ref reader),
            ChunkType.CompiledPhysicalBonesUnknown => ProbablyCustomBonesChunk.Read(ref reader),
            _ =>  UnknownChunk.Read(ref reader)
        };
    }
}