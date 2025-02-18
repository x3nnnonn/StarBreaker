using StarBreaker.Common;

namespace StarBreaker.Chf;

public class SubMaterial
{
    public required NameHash Name { get; init; }
    public required Texture[] Textures { get; init; }
    public required MaterialParam<float>[] MaterialParams { get; init; }
    public required MaterialParam<ColorRgba>[] MaterialColors { get; init; }
    
    public static SubMaterial Read(ref SpanReader reader)
    {
        var key = reader.Read<NameHash>();
        var textureCount = reader.ReadUInt32();
        var textures= new Texture[textureCount];
        for (var i = 0; i < textureCount; i++)
            textures[i] = Texture.Read(ref reader);
        var materialParamCount = (int)reader.ReadUInt64();
        var materialParams = new MaterialParam<float>[materialParamCount];
        for (var i = 0; i < materialParamCount; i++)
            materialParams[i] = MaterialParam<float>.Read(ref reader);
        var materialColorCount = (int)reader.ReadUInt64();
        var materialColors = new MaterialParam<ColorRgba>[materialColorCount];
        for (var i = 0; i < materialColorCount; i++)
            materialColors[i] = MaterialParam<ColorRgba>.Read(ref reader);
        if (reader.Remaining > 0)
            reader.Expect<uint>(5);

        return new SubMaterial
        {
            Name = key,
            Textures = textures,
            MaterialParams = materialParams,
            MaterialColors = materialColors
        };
    }
}