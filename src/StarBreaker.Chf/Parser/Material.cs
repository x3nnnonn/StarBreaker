using StarBreaker.Common;

namespace StarBreaker.Chf.Parser;

public class Material
{
    public required NameHash Name { get; init; }
    public required CigGuid Guid { get; init; }
    public required uint AdditionalParams { get; init; }
    public required SubMaterial[] SubMaterials { get; init; }
    
    public static Material Read(ref SpanReader reader)
    {
        var key = reader.Read<NameHash>();
        var guid = reader.Read<CigGuid>();
        var additionalParams = reader.ReadUInt32();
        reader.Expect(CigGuid.Empty);
        var subMaterialCount = reader.ReadUInt32();
        reader.Expect(5);
        var subMaterials = new SubMaterial[subMaterialCount];
        for (var i = 0; i < subMaterialCount; i++)
            subMaterials[i] = SubMaterial.Read(ref reader);

        return new Material
        {
            Name = key,
            Guid = guid,
            AdditionalParams = additionalParams,
            SubMaterials = subMaterials
        };
    }
}