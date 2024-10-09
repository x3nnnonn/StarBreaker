
using StarBreaker.Common;

namespace StarBreaker.Chf;

public sealed class FaceMaterialChunk
{
    public const uint Key = 0x72129E8E;
    public const uint SpecialKey = 0xa5378a05;
    public const uint Key3 = 0xA87A7C66;
    
    public required MakeupChunk[] Children { get; init; }
    public required FaceInfoChunk Values { get; init; }
    public required FaceColorsChunk Colors { get; init; }

    public static FaceMaterialChunk Read(ref SpanReader reader, HeadMaterialType headMaterial)
    {
        //oddity: when the head material is f11, 05-8A-37-A5 is the key.
        //in *all* other cases, 8E-9E-12-72 is the key. The data seems? to be the same.
        reader.ExpectAny([Key, SpecialKey, Key3]);
        var childCount = reader.Read<uint>();
        var children = new MakeupChunk[childCount];

        for (var i = 0; i < childCount; i++)
        {
            children[i] = MakeupChunk.Read(ref reader);
        }
        
        var floats = FaceInfoChunk.Read(ref reader);
        var colors = FaceColorsChunk.Read(ref reader);
        reader.Expect<uint>(5);

        return new FaceMaterialChunk
        {
            Children = children,
            Values = floats,
            Colors = colors
        };
    }
}