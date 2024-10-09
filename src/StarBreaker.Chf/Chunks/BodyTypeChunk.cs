
using StarBreaker.Common;

namespace StarBreaker.Chf;

public sealed class BodyTypeChunk
{
    public static readonly CigGuid Male = new("25f439d5-146b-4a61-a999-a486dfb68a49");
    public static readonly CigGuid Female = new("d0794a94-efb0-4cad-ad38-2558b4d3c253");
    
    public required BodyType Type { get; init; }
    
    public static BodyTypeChunk Read(ref SpanReader reader)
    {
        var guid = reader.Read<CigGuid>();
        reader.Expect(Guid.Empty);
        var bodyType = guid switch
        {
            _ when guid == Male => BodyType.Male,
            _ when guid == Female => BodyType.Female,
            _ => throw new Exception($"Unexpected BodyTypeChunk guid {guid}")
        };
        
        return new BodyTypeChunk { Type = bodyType };
    }
}

public enum BodyType
{
    Male, 
    Female,
}