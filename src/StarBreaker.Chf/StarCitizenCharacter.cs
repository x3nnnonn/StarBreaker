
using StarBreaker.Common;

namespace StarBreaker.Chf;

public sealed class StarCitizenCharacter
{
    public required BodyTypeChunk BodyType { get; init; }
    public required DnaChunk Dna { get; init; }
    public required BodyChunk Body { get; init; }
    public required HeadMaterialChunk HeadMaterial { get; init; }
    public required FaceMaterialChunk FaceMaterial { get; init; }
    public required List<DyeChunk> Dyes { get; init; }
    public required EyeMaterialChunk EyeMaterial { get; init; }
    public required BodyMaterialChunk BodyMaterial { get; init; }

    public static StarCitizenCharacter FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new SpanReader(data);

        reader.Expect<uint>(2);
        reader.Expect<uint>(7);

        var gender = BodyTypeChunk.Read(ref reader);
        var dnaProperty = DnaChunk.Read(ref reader, gender.Type);
        var totalCount = reader.Read<ulong>();
        var body = BodyChunk.Read(ref reader);
        var headMaterial = HeadMaterialChunk.Read(ref reader);
        var faceMaterial = FaceMaterialChunk.Read(ref reader, headMaterial.Material);

        var props = new List<DyeChunk>();
        while (DyeChunk.DyeKeys.Contains(reader.Peek<uint>()))
        {
            props.Add(DyeChunk.Read(ref reader));
        }

        //sometimes we don't have eye material.
        var eyeMaterial = EyeMaterialChunk.Read(ref reader);
        var bodyMaterialInfo = BodyMaterialChunk.Read(ref reader);

        // if (reader.Remaining != 0)
        //     throw new Exception($"Unexpected data at the end of the file: {reader.Remaining} bytes");

        return new StarCitizenCharacter
        {
            BodyType = gender,
            Dna = dnaProperty,
            Body = body,
            HeadMaterial = headMaterial,
            FaceMaterial = faceMaterial,
            EyeMaterial = eyeMaterial,
            BodyMaterial = bodyMaterialInfo,
            Dyes = props
        };
    }
}