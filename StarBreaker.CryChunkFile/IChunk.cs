using StarBreaker.Common;

namespace StarBreaker.CryChunkFile;

public interface IChunk
{
    static abstract IChunk Read(ref SpanReader reader);
    
    void WriteXmlTo(TextWriter writer);
}