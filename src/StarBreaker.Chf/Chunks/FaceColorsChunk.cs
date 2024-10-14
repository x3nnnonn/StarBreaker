
using StarBreaker.Common;

namespace StarBreaker.Chf;

public sealed class FaceColorsChunk
{
    public required Color HeadColor { get; init; }
    public required Color EyeMakeupColor1 { get; init; }
    public required Color EyeMakeupColor2 { get; init; }
    public required Color EyeMakeupColor3 { get; init; }
    public required Color CheekMakeupColor1 { get; init; }
    public required Color CheekMakeupColor2 { get; init; }
    public required Color CheekMakeupColor3 { get; init; }
    public required Color LipMakeupColor1 { get; init; }
    public required Color LipMakeupColor2 { get; init; }
    public required Color LipMakeupColor3 { get; init; }
    public required uint Data10 { get; init; }
    public required uint Data11 { get; init; }
    public required uint Data12 { get; init; }
    public required uint Data13 { get; init; }
    public required uint Data14 { get; init; }
    public required uint Data15 { get; init; }
    public required uint Data16 { get; init; }
    public required uint Data17 { get; init; }
    public required uint Data18 { get; init; }
    public required uint Data19 { get; init; }
    public required uint Data20 { get; init; }
    public required uint Data21 { get; init; }
    
    public static FaceColorsChunk Read(ref SpanReader reader)
    {
        //note: the uints here are either a bitfield or a bool, not sure.
        reader.Expect<ulong>(0x16);
        
        var data22 = reader.ReadKeyedValue<Color>(0xbd530797);
        var data23 = reader.ReadKeyedValue<Color>(0xb29b1d90);
        var data24 = reader.ReadKeyedValue<Color>(0xe3230e2f);
        var data25 = reader.ReadKeyedValue<Color>(0x2ec0e736);
        var data26 = reader.ReadKeyedValue<Color>(0x1a081a93);
        var data27 = reader.ReadKeyedValue<Color>(0x4bb0092c);
        var data28 = reader.ReadKeyedValue<Color>(0x8653e035);
        var data29 = reader.ReadKeyedValue<Color>(0x7d86e792);
        var data30 = reader.ReadKeyedValue<Color>(0x2c3ef42d);
        var data31 = reader.ReadKeyedValue<Color>(0xe1dd1d34);
        var data32 = reader.ReadKeyedValue<uint>("Makeup1NumTilesU");
        var data33 = reader.ReadKeyedValue<uint>("Makeup1NumTilesV");
        var data34 = reader.ReadKeyedValue<uint>("Makeup1OffsetU");
        var data35 = reader.ReadKeyedValue<uint>("Makeup1OffsetV");
        var data36 = reader.ReadKeyedValue<uint>("Makeup2NumTilesU");
        var data37 = reader.ReadKeyedValue<uint>("Makeup2NumTilesV");
        var data38 = reader.ReadKeyedValue<uint>("Makeup2OffsetU");
        var data39 = reader.ReadKeyedValue<uint>("Makeup2OffsetV");
        var data40 = reader.ReadKeyedValue<uint>("Makeup3NumTilesU");
        var data41 = reader.ReadKeyedValue<uint>("Makeup3NumTilesV");
        var data42 = reader.ReadKeyedValue<uint>("Makeup3OffsetU");
        var data43 = reader.ReadKeyedValue<uint>("Makeup3OffsetV");
        
        return new FaceColorsChunk
        {
            HeadColor = data22,
            EyeMakeupColor1 = data23,
            EyeMakeupColor2 = data24,
            EyeMakeupColor3 = data25,
            CheekMakeupColor1 = data26,
            CheekMakeupColor2 = data27,
            CheekMakeupColor3 = data28,
            LipMakeupColor1 = data29,
            LipMakeupColor2 = data30,
            LipMakeupColor3 = data31,
            Data10 = data32,
            Data11 = data33,
            Data12 = data34,
            Data13 = data35,
            Data14 = data36,
            Data15 = data37,
            Data16 = data38,
            Data17 = data39,
            Data18 = data40,
            Data19 = data41,
            Data20 = data42,
            Data21 = data43
        };
    }
}