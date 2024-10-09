using System.Text.Json.Serialization;
using StarBreaker.Common;

namespace StarBreaker.Chf;

public sealed class DyeValuesChunk
{
    public required uint Param { get; init; }
    public required float DyeAmount { get; init; }
    public required float DyeGradient2 { get; init; }
    //both of these color values are 0-1 floats. top left is 0,0 in the gui.
    //when dye is applied, these will be zero.
    public required float NaturalColorSaturation { get; init; }
    public required float NaturalColorRedness { get; init; }
    public required float DyeVariation { get; init; }
    public required float Unknown { get; init; }
    public required float DyeGradient1 { get; init; }

    public static DyeValuesChunk? Read(ref SpanReader reader)
    {
        //TODO: what is this?
        var k = reader.Read<uint>();
        reader.Expect(0);
        var count = reader.Read<ulong>();

        if (count == 0)
            return null;
        if (count != 7)
            throw new Exception($"Expected 7 floats, got {count}");

        var f01 = reader.ReadKeyedValue<float>(0x4af6c15a);
        var f02 = reader.ReadKeyedValue<float>(0xc3370bd9);
        var f03 = reader.ReadKeyedValue<float>(0xb9fa00a3);
        var f04 = reader.ReadKeyedValue<float>(0x62fbf0af);
        var f05 = reader.ReadKeyedValue<float>(0x06084076);
        var f06 = reader.ReadKeyedValue<float>(0xa59aa7c8);
        var f07 = reader.ReadKeyedValue<float>(0x027eb674);

        return new DyeValuesChunk
        {
            Param = k,
            DyeAmount = f01,
            DyeGradient2 = f02,
            NaturalColorSaturation = f03,
            NaturalColorRedness = f04,
            DyeVariation = f05,
            Unknown = f06,
            DyeGradient1 = f07
        };
    }
}