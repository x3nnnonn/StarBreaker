using StarBreaker.Common;

namespace StarBreaker.CryChunkFile;

public class UnknownChunk : IChunk
{
    public static IChunk Read(ref SpanReader reader)
    {
        return new UnknownChunk();
    }

    public void WriteXmlTo(TextWriter writer)
    {
    }
}