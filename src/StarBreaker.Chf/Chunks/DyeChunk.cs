using System.Text.Json.Serialization;
using StarBreaker.Common;

namespace StarBreaker.Chf;

public sealed class DyeChunk
{
    public static readonly uint[] DyeKeys =
    [
        0x6C836947, 
        0x078AC8BD, 
        0x9B274D93
    ];
    
    public required DyeType DyeType { get; init; }
    public required uint Unknown { get; init; }
    public required DyeValuesChunk? Values { get; init; }
    public required Color? RootDyeColor { get; init; }
    public required Color? TipDyeColor { get; init; }
    
    public static DyeChunk Read(ref SpanReader reader)
    {
        var key = reader.Read<uint>();
        var dyeType = key switch
        {
            0x6C836947 => DyeType.Hair,
            0x078AC8BD => DyeType.Eyebrow,
            0x9B274D93 => DyeType.Beard,
            _ => throw new Exception($"Unexpected key: {key:X}")
        };
        
        reader.Expect(Guid.Empty);
        var id = reader.Read<uint>();
        reader.Expect(Guid.Empty);
        reader.Expect(1);
        reader.Expect(5);
        var floats = DyeValuesChunk.Read(ref reader);
        var colors = ColorsChunk.Read(ref reader);
        reader.Expect(5);
        
        return new DyeChunk
        {
            DyeType = dyeType,
            Unknown = id,
            Values = floats,
            RootDyeColor = colors.Color02,
            TipDyeColor = colors.Color01
        };
    }
}

public enum DyeType
{
    Hair,
    Eyebrow,
    Beard,
}