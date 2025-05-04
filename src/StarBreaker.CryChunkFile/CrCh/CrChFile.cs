using System.Diagnostics.CodeAnalysis;
using StarBreaker.Common;

namespace StarBreaker.CryChunkFile;

public sealed class CrChFile
{
    private const uint CrChMagic = 0x68437243; //"CrCh"u8

    public ChunkHeaderCrCh[] Headers { get; private set; }
    public byte[][] Chunks { get; private set; }

    public static bool TryRead(Span<byte> bytes, [NotNullWhen(true)] out CrChFile? ivoFile)
    {
        var br = new SpanReader(bytes);
        
        ivoFile = null;
        if (br.Length < 4)
            return false;

        var magic = br.Read<uint>();
        if (magic != CrChMagic)
            return false;
        
        var version = br.Read<uint>();
        if (version != 0x746)
            return false;

        ivoFile = new CrChFile(ref br);
        return true;
    }

    private CrChFile(ref SpanReader reader)
    {
        //we already read the magic and version
        var chunkCount = reader.Read<uint>();
        var _chunkTableOffset = reader.Read<uint>();

        Headers = reader.ReadSpan<ChunkHeaderCrCh>((int)chunkCount).ToArray();
        Chunks = ReadChunks(Headers,ref  reader);
    }
    
    private static byte[][] ReadChunks(ReadOnlySpan<ChunkHeaderCrCh> headers, ref SpanReader reader)
    {
        var chunks = new byte[headers.Length][];
        for (var i = 0; i < headers.Length; i++)
        {
            var header = headers[i];
            reader.Seek((int)header.Offset);
            chunks[i] = reader.ReadBytes((int)header.Size).ToArray();
        }

        return chunks;
    }
}