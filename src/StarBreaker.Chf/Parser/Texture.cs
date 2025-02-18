using StarBreaker.Common;

namespace StarBreaker.Chf.Parser;

public class Texture
{
    public required byte Index { get; init; }
    public required CigGuid Guid { get; init; }
    
    public static Texture Read(ref SpanReader reader)
    {
        reader.Expect(0);
        var texIndex = reader.ReadByte();
        var texGuid = reader.Read<CigGuid>();
        
        return new Texture
        {
            Index = texIndex,
            Guid = texGuid
        };
    }
}