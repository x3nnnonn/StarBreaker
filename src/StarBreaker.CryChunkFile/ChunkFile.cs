using System.Diagnostics;
using StarBreaker.Common;

namespace StarBreaker.CryChunkFile;

public sealed class ChunkFile
{
    private readonly byte[] _data;
    private const uint CrChMagic = 0x68437243; //"CrCh"u8
    private const uint IvoMagic = 0x6F766923; //"#ivo"u8
    private static readonly HashSet<ChunkTypeIvo> _unknownKeys = new();
    private static readonly HashSet<ChunkTypeChCf> _unknownKeys2 = new();

    private ChunkFile(byte[] data)
    {
        _data = data;
    }
    
    public static bool IsChunkFile(string path)
    {
        using var stream = new BinaryReader(File.OpenRead(path));
        
        if (stream.BaseStream.Length < 4)
            return false;
        
        return stream.ReadUInt32() is CrChMagic or IvoMagic;
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

        switch (signature)
        {
            case CrChMagic:
                WriteChCfTo(writer, ref reader);
                break;
            case IvoMagic:
                WriteIvoTo(writer, ref reader);
                break;
            default:
                throw new Exception("Invalid signature");
        }
    }

    private static void WriteChCfTo(TextWriter writer, ref SpanReader reader)
    {
        var version = reader.ReadUInt32();
        if (version != 0x746)
            throw new Exception("Invalid version");

        var chunkCount = reader.ReadUInt32();
        var chunkTableOffset = reader.ReadUInt32();
        Debug.Assert(chunkTableOffset == reader.Position);
        var headers = reader.ReadSpan<ChunkHeaderCrCh>((int)chunkCount);

        var chunks = new IChunk[chunkCount];
        for (var i = 0; i < headers.Length; i++)
        {
            ref readonly var header = ref headers[i];
            //Debug.Assert(header.Offset == reader.GetPosition());
            //reader.Seek((int)header.Offset);

            if (Enum.IsDefined(typeof(ChunkTypeChCf), header.ChunkType) == false)
            {
                // var cast = (ChunkTypeIvo)((uint)header.ChunkType + 0xCCCBF000);
                // if (Enum.IsDefined(typeof(ChunkTypeIvo), cast))
                // {
                //     Console.WriteLine($"Chf chunk type:  0x{header.ChunkType:X} | {header.ChunkType} => 0x{cast:X} | {cast}");
                //     continue;
                // }

                if (_unknownKeys2.Add(header.ChunkType))
                    Console.WriteLine($"Unknown Chf chunk type:  0x{header.ChunkType:X} | {header.ChunkType}");
                
                continue;
            }

            //chunks[i] = ReadChunk(ref reader, header.ChunkType);
        }
    }

    private void WriteIvoTo(TextWriter writer, ref SpanReader reader)
    {
        var version = reader.ReadUInt32();
        if (version != 0x900)
            throw new Exception("Invalid version");

        var chunkCount = reader.ReadUInt32();
        var chunkTableOffset = reader.ReadUInt32();
        Debug.Assert(chunkTableOffset == reader.Position);
        var headers = reader.ReadSpan<ChunkHeaderIvo>((int)chunkCount);
        Span<int> lengths = stackalloc int[headers.Length];
        GetChunkLengths(headers, lengths);

        var chunks = new IChunk[chunkCount];
        for (var i = 0; i < headers.Length; i++)
        {
            ref readonly var header = ref headers[i];
            if (Enum.IsDefined(typeof(ChunkTypeIvo), header.ChunkType) == false)
            {
                if (_unknownKeys.Add(header.ChunkType))
                    Console.WriteLine($"Unknown Ivo chunk type:  0x{header.ChunkType:X} | {header.ChunkType}");
                
                continue;
            }

            chunks[i] = ReadChunk(ref reader, header.ChunkType);
        }
    }

    private void GetChunkLengths(ReadOnlySpan<ChunkHeaderIvo> headers, Span<int> lengths)
    {
        for (var i = 0; i < headers.Length; i++)
        {
            ref readonly var header = ref headers[i];
            if (i == headers.Length - 1)
                lengths[i] = _data.Length - (int)header.Offset;
            else
                lengths[i] = (int)(headers[i + 1].Offset - header.Offset);
        }
    }

    private static IChunk ReadChunk(ref SpanReader reader, ChunkTypeIvo chunkTypeIvo)
    {
        return chunkTypeIvo switch
        {
            //TODO: fill in
            ChunkTypeIvo.CompiledBonesIvo320 => CompiledBoneChunk.Read(ref reader),
            ChunkTypeIvo.MtlNameIvo320 => MaterialNameChunk.Read(ref reader),
            ChunkTypeIvo.CompiledPhysicalBonesUnknown => ProbablyCustomBonesChunk.Read(ref reader),
            _ => UnknownChunk.Read(ref reader)
        };
    }
}