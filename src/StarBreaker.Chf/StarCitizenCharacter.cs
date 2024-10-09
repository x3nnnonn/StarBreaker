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
        var objs = new List<object>();
        for (ulong i = 0; i < totalCount; i++)
        {
            var key = reader.Peek<uint>();
            switch (key)
            {
                case BodyChunk.Key:
                {
                    body = BodyChunk.Read(ref reader);
                    objs.Add(body);
                    break;
                }
                case HeadMaterialChunk.Key:
                {
                    headMaterial = HeadMaterialChunk.Read(ref reader);
                    objs.Add(headMaterial);
                    break;
                }
                case EyeMaterialChunk.Key:
                {
                    eyeMaterial = EyeMaterialChunk.Read(ref reader);
                    objs.Add(eyeMaterial);
                    break;
                }
                case BodyMaterialChunk.Key:
                {
                    bodyMaterial = BodyMaterialChunk.Read(ref reader);
                    objs.Add(bodyMaterial);
                    break;
                }
                default:
                {
                    if (FaceMaterialChunk.Keys.Contains(key))
                    {
                        faceMaterial = FaceMaterialChunk.Read(ref reader);
                        objs.Add(faceMaterial);
                        //TODO: do we need to scan for dye chunks here?
                        break;
                    }

                    if (DyeChunk.DyeKeys.Contains(key))
                    {
                        var dye = DyeChunk.Read(ref reader);
                        props.Add(dye);
                        objs.Add(dye);
                        break;
                    }

                    objs.Add(null);
                    Console.WriteLine($"Unexpected key: {key}");
                    break;
                }
            }
        }

        //var body = BodyChunk.Read(ref reader);
        //var headMaterial = HeadMaterialChunk.Read(ref reader);
        //var faceMaterial = FaceMaterialChunk.Read(ref reader);
        while (reader.Remaining > 0 && DyeChunk.DyeKeys.Contains(reader.Peek<uint>()))
        {
            try
            {
                props.Add(DyeChunk.Read(ref reader));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        while (reader.Remaining > 0)
        {
            var key = reader.Peek<uint>();
            if (key == EyeMaterialChunk.Key)
            {
                eyeMaterial = EyeMaterialChunk.Read(ref reader);
                break;
            }

            if (key == BodyMaterialChunk.Key)
            {
                bodyMaterial = BodyMaterialChunk.Read(ref reader);
                break;
            }
            
            Console.WriteLine($"Unexpected key: {key:X8}");
            throw new Exception($"Unexpected key: {key:X8}");
        }


        //Console.WriteLine($"Unexpected data at the end of the file: {reader.Remaining} bytes.Next Key: 0x{reader.Peek<uint>():X8}");


        //sometimes we don't have eye material.
        //var eyeMaterial = EyeMaterialChunk.Read(ref reader);
        //var bodyMaterial = BodyMaterialChunk.Read(ref reader);

        // if (reader.Remaining != 0)
        // {
        //     var props2 = new List<DyeChunk>();
        //
        //     try
        //     {
        //         Console.WriteLine($"Unexpected data at the end of the file: {reader.Remaining} bytes");
        //         reader.Expect(5);
        //         var eyemat = EyeMaterialChunk.Read(ref reader);
        //         while (DyeChunk.DyeKeys.Contains(reader.Peek<uint>()))
        //         {
        //             props2.Add(DyeChunk.Read(ref reader));
        //         }
        //
        //         Console.WriteLine($"Unexpected data at the end of the file: {reader.Remaining} bytes");
        //     }
        //     catch (Exception e)
        //     {
        //         Console.WriteLine(e);
        //         throw;
        //     }
        // }

        return new StarCitizenCharacter
        {
            BodyType = gender,
            Dna = dnaProperty,
            Body = body,
            HeadMaterial = headMaterial,
            FaceMaterial = faceMaterial,
            EyeMaterial = eyeMaterial,
            BodyMaterial = bodyMaterial,
            Dyes = props
        };
    }
}