
using StarBreaker.Common;

namespace StarBreaker.Chf;

public sealed class EyebrowChunk
{
    public static readonly uint Key = ItemPortKeys.GetUIntKey("eyebrow_itemport");
    
    public required EyebrowType EyebrowType { get; init; }
    public required ulong ChildCount { get; init; }
    
    public static EyebrowChunk Read(ref SpanReader reader)
    {
        reader.Expect(Key);
        var guid = reader.Read<CigGuid>();
        var childCount = reader.Read<ulong>();
        
        var type = guid switch
        {
            _ when guid == Brows01 => EyebrowType.Brows01,
            _ when guid == Brows02 => EyebrowType.Brows02,
            _ when guid == Brows03 => EyebrowType.Brows03,
            _ when guid == Brows04 => EyebrowType.Brows04,
            _ when guid == Brows05 => EyebrowType.Brows05,
            _ when guid == Brows06 => EyebrowType.Brows06,
            _ =>  EyebrowType.Unknown
        };

        return new EyebrowChunk
        {
            EyebrowType = type,
            ChildCount = childCount
        };
    }
    
    public static readonly CigGuid Brows01 = new("89ec0bbc-7daf-4b09-a98d-f8dd8df32305");
    public static readonly CigGuid Brows02 = new("c40183e4-659c-4e4e-8f96-70b33a3b9d67");
    public static readonly CigGuid Brows03 = new("6606176a-bfc4-4d24-a40a-b554fcfb8c7e");
    public static readonly CigGuid Brows04 = new("41a65deb-4a4c-425c-8825-e6d264ecdd4b");
    public static readonly CigGuid Brows05 = new("a074880a-6df2-4996-89e2-3e204a2790c2");
    public static readonly CigGuid Brows06 = new("03270dfe-71be-45ee-b51a-fb1dd7e67ba1");
}

public enum EyebrowType
{
    Unknown = -1,
    None,
    Brows01,
    Brows02,
    Brows03,
    Brows04,
    Brows05,
    Brows06
}