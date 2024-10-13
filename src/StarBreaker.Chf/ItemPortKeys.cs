using System.Collections.Frozen;
using System.Text;
using StarBreaker.Common;

namespace StarBreaker.Chf;

public static class ItemPortKeys
{
    private static readonly string[] _itemPortNames =
    [
        "body_itemport",
        "head_itemport",
        "eyes_itemport",
        "hair_itemport",
        "material_variant",
        "eyebrow_itemport",
        "eyelashes_itemport",
        "beard_itemport",
        "stubble_itemport",
        "piercings_nose_itemport",
        "piercings_eyebrows_itemport",
        "piercings_l_ear_itemport",
        "piercings_r_ear_itemport",
        "piercings_mouth_itemport",
        "DyeShift",
        "DyeAmount",
        "TattooAge",
        "DyeFadeout",
        "BaseMelanin",
        "FrecklesAmount",
        "SunSpotsAmount",
        "Makeup1Opacity",
        "Makeup1OffsetU",
        "Makeup1OffsetV",
        "Makeup2Opacity",
        "Makeup2OffsetU",
        "Makeup2OffsetV",
        "Makeup3Opacity",
        "Makeup3OffsetU",
        "Makeup3OffsetV",
        "FrecklesOpacity",
        "SunSpotsOpacity",
        "Makeup1NumTilesU",
        "Makeup1NumTilesV",
        "Makeup2NumTilesU",
        "Makeup2NumTilesV",
        "Makeup3NumTilesU",
        "Makeup3NumTilesV",
        "Makeup1MetalnessR",
        "Makeup1MetalnessG",
        "Makeup1MetalnessB",
        "Makeup2MetalnessR",
        "Makeup2MetalnessG",
        "Makeup2MetalnessB",
        "Makeup3MetalnessR",
        "Makeup3MetalnessG",
        "Makeup3MetalnessB",
        "TattooHueRotation",
        "BaseMelaninRedness",
        "Makeup1SmoothnessR",
        "Makeup1SmoothnessG",
        "Makeup1SmoothnessB",
        "Makeup2SmoothnessR",
        "Makeup2SmoothnessG",
        "Makeup2SmoothnessB",
        "Makeup3SmoothnessR",
        "Makeup3SmoothnessG",
        "Makeup3SmoothnessB",
        "DyePigmentVariation",
        "BaseMelaninVariation",
    ];
    
    private static readonly FrozenDictionary<uint, string> _reverseCrc32c;
    
    static ItemPortKeys()
    {
        var dict = new Dictionary<uint, string>();
        foreach (var name in _itemPortNames)
        {
            var bytes = Encoding.ASCII.GetBytes(name).AsSpan();
            var crc = Crc32c.FromSpan(bytes);
            dict[crc] = name;
        }
        
        //add here the ones we are not able to find, but roughly know the meaning of.
        dict[0x27424D58] = "body_material";
        dict[0xA047885E] = "eye_material";
        dict[0xA98BEB34] = "head_material";

        _reverseCrc32c = dict.ToFrozenDictionary();
    }
    
    public static string GetKey(uint key)
    {
        return _reverseCrc32c[key];
    }

    public static bool TryGetKey(uint key, out string name)
    {
        return _reverseCrc32c.TryGetValue(key, out name);
    }

    public static uint GetUIntKey(string key)
    {
        var count = Encoding.ASCII.GetByteCount(key);
        Span<byte> bytes = stackalloc byte[count];
        Encoding.ASCII.GetBytes(key, bytes);
        return Crc32c.FromSpan(bytes);
    }
}