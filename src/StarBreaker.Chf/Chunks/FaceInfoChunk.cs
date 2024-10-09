
using StarBreaker.Common;

namespace StarBreaker.Chf;

public sealed class FaceInfoChunk
{
    public required float FreckleAmount { get; init; }
    public required float FreckleOpacity { get; init; }
    public required float SunSpotsAmount { get; init; }
    public required float SunSpotOpacity { get; init; }
    public required float EyeMetallic1 { get; init; }
    public required float EyeMetallic2 { get; init; }
    public required float EyeMetallic3 { get; init; }
    public required float EyeSmoothness1 { get; init; }
    public required float EyeSmoothness2 { get; init; }
    public required float EyeSmoothness3 { get; init; }
    public required float EyeOpacity { get; init; }
    public required float CheekMetallic1 { get; init; }
    public required float CheekMetallic2 { get; init; }
    public required float CheekMetallic3 { get; init; }
    public required float CheekSmoothness1 { get; init; }
    public required float CheekSmoothness2 { get; init; }
    public required float CheekSmoothness3 { get; init; }
    public required float CheekOpacity { get; init; }
    public required float LipMetallic1 { get; init; }
    public required float LipMetallic2 { get; init; }
    public required float LipMetallic3 { get; init; }
    public required float LipSmoothness1 { get; init; }
    public required float LipSmoothness2 { get; init; }
    public required float LipSmoothness3 { get; init; }
    public required float LipOpacity { get; init; }
    
    public static FaceInfoChunk Read(ref SpanReader reader)
    {
        var count = reader.Read<uint>();
        if (count != 25 && count != 27)
            throw new Exception("Unexpected FaceInfoChunk count: " + count);
        reader.Expect<uint>(0);
        
        var freckleAmount = reader.ReadKeyedValue<float>(0xe87727e2);
        var freckleOpacity = reader.ReadKeyedValue<float>(0x9361cb58);
        var sunSpotsAmount = reader.ReadKeyedValue<float>(0x554ad20f);
        var sunSpotOpacity = reader.ReadKeyedValue<float>(0xcfc41264);
        var eyeMetallic1 = reader.ReadKeyedValue<float>(0xb95883b0);
        var eyeMetallic2 = reader.ReadKeyedValue<float>(0x9cf750c3);
        var eyeMetallic3 = reader.ReadKeyedValue<float>(0xa90644df);
        var eyeSmoothness1 = reader.ReadKeyedValue<float>(0xc871a987);
        var eyeSmoothness2 = reader.ReadKeyedValue<float>(0xedde7af4);
        var eyeSmoothness3 = reader.ReadKeyedValue<float>(0xd82f6ee8);
        var eyeOpacity = reader.ReadKeyedValue<float>(0xcae526ba);
        var cheekMetallic1 = reader.ReadKeyedValue<float>(0x0526ed02);
        var cheekMetallic2 = reader.ReadKeyedValue<float>(0x20893e71);
        var cheekMetallic3 = reader.ReadKeyedValue<float>(0x15782a6d);
        var cheekSmoothness1 = reader.ReadKeyedValue<float>(0x9be3d5d7);
        var cheekSmoothness2 = reader.ReadKeyedValue<float>(0xbe4c06a4);
        var cheekSmoothness3 = reader.ReadKeyedValue<float>(0x8bbd12b8);
        var cheekOpacity = reader.ReadKeyedValue<float>(0x11a1a1d3);
        var lipMetallic1 = reader.ReadKeyedValue<float>(0x92571ac3);
        var lipMetallic2 = reader.ReadKeyedValue<float>(0xb7f8c9b0);
        var lipMetallic3 = reader.ReadKeyedValue<float>(0x8209ddac);
        var lipSmoothness1 = reader.ReadKeyedValue<float>(0xaa9201e7);
        var lipSmoothness2 = reader.ReadKeyedValue<float>(0x8f3dd294);
        var lipSmoothness3 = reader.ReadKeyedValue<float>(0xbaccc688);
        var lipOpacity = reader.ReadKeyedValue<float>(0x589ddcf4);
        if (count == 27)
        {
            var unknownExtraFloat = reader.ReadKeyedValue<float>(0x64C1127);
            var unknownExtraFloat2 = reader.ReadKeyedValue<float>(0xEC67C07D);
            
        }
        return new FaceInfoChunk
        {
            FreckleAmount = freckleAmount,
            FreckleOpacity = freckleOpacity,
            SunSpotsAmount = sunSpotsAmount,
            SunSpotOpacity = sunSpotOpacity,
            EyeMetallic1 = eyeMetallic1,
            EyeMetallic2 = eyeMetallic2,
            EyeMetallic3 = eyeMetallic3,
            EyeSmoothness1 = eyeSmoothness1,
            EyeSmoothness2 = eyeSmoothness2,
            EyeSmoothness3 = eyeSmoothness3,
            EyeOpacity = eyeOpacity,
            CheekMetallic1 = cheekMetallic1,
            CheekMetallic2 = cheekMetallic2,
            CheekMetallic3 = cheekMetallic3,
            CheekSmoothness1 = cheekSmoothness1,
            CheekSmoothness2 = cheekSmoothness2,
            CheekSmoothness3 = cheekSmoothness3,
            CheekOpacity = cheekOpacity,
            LipMetallic1 = lipMetallic1,
            LipMetallic2 = lipMetallic2,
            LipMetallic3 = lipMetallic3,
            LipSmoothness1 = lipSmoothness1,
            LipSmoothness2 = lipSmoothness2,
            LipSmoothness3 = lipSmoothness3,
            LipOpacity = lipOpacity,
        };
    }
}