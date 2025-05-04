using System.Diagnostics.CodeAnalysis;
using StarBreaker.Common;

namespace StarBreaker.CryChunkFile;

public sealed class IvoFile
{
    private const uint IvoMagic = 0x6F766923; //"#ivo"u8
        
    public ChunkHeaderIvo[] Headers { get; private set; }
    public IChunk[] Chunks { get; private set; }

    public static bool TryRead(Span<byte> bytes, [NotNullWhen(true)] out IvoFile? ivoFile)
    {
        var br = new SpanReader(bytes, 0);
        
        ivoFile = null;
        if (br.Length < 4)
            return false;

        var magic = br.Read<uint>();
        if (magic != IvoMagic)
            return false;
        
        var version = br.Read<uint>();
        if (version != 0x900)
            return false;

        ivoFile = new IvoFile(ref br);
        return true;
    }

    private IvoFile(ref SpanReader reader)
    {
        //we already read the magic and version
        var chunkCount = reader.Read<uint>();
        var chunkTableOffset = reader.Read<uint>();

        Headers = reader.ReadSpan<ChunkHeaderIvo>((int)chunkCount).ToArray();
        Chunks = ReadChunks(Headers,ref  reader);
    }
    
    private static IChunk[] ReadChunks(ReadOnlySpan<ChunkHeaderIvo> headers, ref SpanReader reader)
    {
        var chunks = new IChunk[headers.Length];
        for (var i = 0; i < headers.Length; i++)
        {
            var header = headers[i];
            reader.Seek(header.Offset);
            chunks[i] = ReadChunk(header, ref reader);
        }

        return chunks;
    }
    
    private static IChunk ReadChunk(ChunkHeaderIvo header, ref SpanReader reader)
    {
        return header.ChunkType switch
        {
            ChunkTypeIvo.CompiledBonesIvo320 => CompiledBoneChunk.Read(ref reader),
            ChunkTypeIvo.MtlNameIvo320 => MaterialNameChunk.Read(ref reader),
            ChunkTypeIvo.PhysicalHierarchy => PhysicalHierarchyChunk.Read(ref reader),
            ChunkTypeIvo.Unknown05 => UnknownChunk5.Read(ref reader),
            ChunkTypeIvo.MeshIvo320 => MeshIvo320.Read(ref reader),
            ChunkTypeIvo.IvoSkin2 => IvoSkin2.Read(ref reader),
            ChunkTypeIvo.ExportFlags => ExportFlagsChunk.Read(ref reader),
            ChunkTypeIvo.NodeMeshCombos => NodeMeshCombosChunk.Read(ref reader),
            _ => UnknownChunk.Read(ref reader)
        };
    }
}