
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
        
        var freckleAmount = reader.ReadKeyedValue<float>("FrecklesAmount");
        var freckleOpacity = reader.ReadKeyedValue<float>("FrecklesOpacity");
        var sunSpotsAmount = reader.ReadKeyedValue<float>("SunSpotsAmount");
        var sunSpotOpacity = reader.ReadKeyedValue<float>("SunSpotsOpacity");
        var eyeMetallic1 = reader.ReadKeyedValue<float>("Makeup1MetalnessR");
        var eyeMetallic2 = reader.ReadKeyedValue<float>("Makeup1MetalnessG");
        var eyeMetallic3 = reader.ReadKeyedValue<float>("Makeup1MetalnessB");
        var eyeSmoothness1 = reader.ReadKeyedValue<float>("Makeup1SmoothnessR");
        var eyeSmoothness2 = reader.ReadKeyedValue<float>("Makeup1SmoothnessG");
        var eyeSmoothness3 = reader.ReadKeyedValue<float>("Makeup1SmoothnessB");
        var eyeOpacity = reader.ReadKeyedValue<float>("Makeup1Opacity");
        var cheekMetallic1 = reader.ReadKeyedValue<float>("Makeup2MetalnessR");
        var cheekMetallic2 = reader.ReadKeyedValue<float>("Makeup2MetalnessG");
        var cheekMetallic3 = reader.ReadKeyedValue<float>("Makeup2MetalnessB");
        var cheekSmoothness1 = reader.ReadKeyedValue<float>("Makeup2SmoothnessR");
        var cheekSmoothness2 = reader.ReadKeyedValue<float>("Makeup2SmoothnessG");
        var cheekSmoothness3 = reader.ReadKeyedValue<float>("Makeup2SmoothnessB");
        var cheekOpacity = reader.ReadKeyedValue<float>("Makeup2Opacity");
        var lipMetallic1 = reader.ReadKeyedValue<float>("Makeup3MetalnessR");
        var lipMetallic2 = reader.ReadKeyedValue<float>("Makeup3MetalnessG");
        var lipMetallic3 = reader.ReadKeyedValue<float>("Makeup3MetalnessB");
        var lipSmoothness1 = reader.ReadKeyedValue<float>("Makeup3SmoothnessR");
        var lipSmoothness2 = reader.ReadKeyedValue<float>("Makeup3SmoothnessG");
        var lipSmoothness3 = reader.ReadKeyedValue<float>("Makeup3SmoothnessB");
        var lipOpacity = reader.ReadKeyedValue<float>("Makeup3Opacity");
        if (count == 27)
        {
            var unknownExtraFloat = reader.ReadKeyedValue<float>("TattooAge");
            var unknownExtraFloat2 = reader.ReadKeyedValue<float>("TattooHueRotation");
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