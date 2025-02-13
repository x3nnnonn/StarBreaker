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
        var dnaProperty = DnaChunk.Read(ref reader);
        var totalCount = reader.Read<ulong>();

        BodyChunk? body = null;
        HeadMaterialChunk? headMaterial = null;
        FaceMaterialChunk? faceMaterial = null;
        EyeMaterialChunk? eyeMaterial = null;
        BodyMaterialChunk? bodyMaterial = null;
        var props = new List<DyeChunk>();
        var objs = new List<object?>();
        while (reader.Remaining > 0)
        {
            var key = reader.Peek<uint>();
            
            if (key == BodyChunk.Key)
            {
                body = BodyChunk.Read(ref reader);
                objs.Add(body);
            }
            else if (key == HeadMaterialChunk.Key)
            {
                headMaterial = HeadMaterialChunk.Read(ref reader);
                objs.Add(headMaterial);
            }
            else if (key == EyeMaterialChunk.Key)
            {
                eyeMaterial = EyeMaterialChunk.Read(ref reader);
                objs.Add(eyeMaterial);
            }
            else if (key == BodyMaterialChunk.Key)
            {
                bodyMaterial = BodyMaterialChunk.Read(ref reader);
                objs.Add(bodyMaterial);
            }
            else if (FaceMaterialChunk.Keys.Contains(key))
            {
                faceMaterial = FaceMaterialChunk.Read(ref reader);
                objs.Add(faceMaterial);
            }
            else if (DyeChunk.DyeKeys.Contains(key))
            {
                var dye = DyeChunk.Read(ref reader);
                props.Add(dye);
                objs.Add(dye);
            }
            else
            {
                //0x9D8B687A
                objs.Add(null);
                Console.WriteLine($"Unexpected key: {key}");
            }
        }

        return new StarCitizenCharacter
        {
            BodyType = gender,
            Dna = dnaProperty,
            Body = body ?? throw new Exception("Body not found"),
            HeadMaterial = headMaterial ?? throw new Exception("Head material not found"),
            FaceMaterial = faceMaterial ?? throw new Exception("Face material not found"),
            EyeMaterial = eyeMaterial ?? throw new Exception("Eye material not found"),
            BodyMaterial = bodyMaterial ?? throw new Exception("Body material not found"),
            Dyes = props
        };
    }
}