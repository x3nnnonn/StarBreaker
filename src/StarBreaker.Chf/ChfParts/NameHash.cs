using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using StarBreaker.Common;

namespace StarBreaker.Chf;

[DebuggerDisplay("{Value}")]
public readonly record struct NameHash
{
    private readonly uint _value;

    public string Value => TryGetName(out var name) ? name : $"0x{_value:X8}";

    private NameHash(uint value)
    {
        _value = value;
    }

    public static NameHash FromString(string name) => new(Crc32c.FromString(name));

    private static readonly Dictionary<uint, string> _names;

    public bool TryGetName([NotNullWhen(true)] out string? name) => _names.TryGetValue(_value, out name);

    static NameHash()
    {
        string[] names =
        [
            "BaseMelanin"                   , //0xB9FA00A3
            "BaseMelaninRedness"            , //0x62FBF0AF
            "BaseMelaninVariation"          , //0xA59AA7C8
            "DyeAmount"                     , //0x4AF6C15A
            "DyeFadeout"                    , //0xC3370BD9
            "DyePigmentVariation"           , //0x06084076
            "DyeShift"                      , //0x027EB674
            "FrecklesAmount"                , //0xE87727E2
            "FrecklesOpacity"               , //0x9361CB58
            "Makeup1MetalnessB"             , //0xA90644DF
            "Makeup1MetalnessG"             , //0x9CF750C3
            "Makeup1MetalnessR"             , //0xB95883B0
            "Makeup1NumTilesU"              , //0x64A583EC
            "Makeup1NumTilesV"              , //0x77F57018
            "Makeup1OffsetU"                , //0xE9F3E598
            "Makeup1OffsetV"                , //0xFAA3166C
            "Makeup1Opacity"                , //0xCAE526BA
            "Makeup1SmoothnessB"            , //0xD82F6EE8
            "Makeup1SmoothnessG"            , //0xEDDE7AF4
            "Makeup1SmoothnessR"            , //0xC871A987
            "Makeup2MetalnessB"             , //0x15782A6D
            "Makeup2MetalnessG"             , //0x20893E71
            "Makeup2MetalnessR"             , //0x0526ED02
            "Makeup2NumTilesU"              , //0x3CB379F2
            "Makeup2NumTilesV"              , //0x2FE38A06
            "Makeup2OffsetU"                , //0x32B762F1
            "Makeup2OffsetV"                , //0x21E79105
            "Makeup2Opacity"                , //0x11A1A1D3
            "Makeup2SmoothnessB"            , //0x8BBD12B8
            "Makeup2SmoothnessG"            , //0xBE4C06A4
            "Makeup2SmoothnessR"            , //0x9BE3D5D7
            "Makeup3MetalnessB"             , //0x8209DDAC
            "Makeup3MetalnessG"             , //0xB7F8C9B0
            "Makeup3MetalnessR"             , //0x92571AC3
            "Makeup3NumTilesU"              , //0xF7E50257
            "Makeup3NumTilesV"              , //0xE4B5F1A3
            "Makeup3OffsetU"                , //0x7B8B1FD6
            "Makeup3OffsetV"                , //0x68DBEC22
            "Makeup3Opacity"                , //0x589DDCF4
            "Makeup3SmoothnessB"            , //0xBACCC688
            "Makeup3SmoothnessG"            , //0x8F3DD294
            "Makeup3SmoothnessR"            , //0xAA9201E7
            "SunSpotsAmount"                , //0x554AD20F
            "SunSpotsOpacity"               , //0xCFC41264
            "TattooAge"                     , //0x064C1127
            "TattooHueRotation"             , //0xEC67C07D
            "beard_itemport"                , //0x98EFBB1C
            "body_itemport"                 , //0xAB6341AC
            "body_m"                        , //0x73C979A9
            "dna matrix 1.0"                , //0xFCD09394
            "eyebrow_itemport"              , //0x1787EE22
            "eyelashes_itemport"            , //0x190B04E2
            "eyes_itemport"                 , //0xC5BB5550
            "f_limbs_m"                     , //0x8A5B66DB
            "f_torso_m"                     , //0x316B6E4C
            "female23"                      , //0xA87A7C66
            "female26"                      , //0x9D8B687A
            "female27"                      , //0x6FE0EB79
            "hair_itemport"                 , //0x13601A95
            "head_itemport"                 , //0x47010DB9
            "limbs_m"                       , //0xA41FA12C
            "material_variant"              , //0xE7809D46
            "piercings_eyebrows_itemport"   , //0xc8fff8ae
            "piercings_l_ear_itemport"      , //0x6958D171
            "piercings_mouth_itemport"      , //0xE59EBF06
            "piercings_nose_itemport"       , //0x45FBEF91
            "piercings_r_ear_itemport"      , //0x6D6DE693
            "protos_human_female_face_t1_pu", //0x9EF4EB54
            "protos_human_male_face_t1_pu"  , //0xDD6C67F6
            "shader_Head"                   , //0xA5378A05
            "shader_eyeInner"               , //0x9736C44B
            "shader_eyeinner"               , //0x8C9E711C
            "shader_head"                   , //0x72129E8E
            "stubble_itemport"              , //0x01C113C7
            "universal_scalp_itemport"      , //0xddfa667b
        ];

        _names = names.ToDictionary(Crc32c.FromString);
        
        _names.Add(0xa98beb34, "Head Material");
        _names.Add(0x6c836947, "HairDyeMaterial");
        _names.Add(0x078ac8bd, "EyebrowDyeMaterial");
        _names.Add(0xa047885e, "EyeMaterial");
        _names.Add(0x9b274d93, "BeardDyeMaterial");
        _names.Add(0x27424d58, "BodyMaterial");
        _names.Add(0xa8770416, "DyeMaterial");

        _names.Add(0xbd530797, "BodyColor");

        _names.Add(0xb29b1d90, "EyeMakeupColor1");
        _names.Add(0xe3230e2f, "EyeMakeupColor2");
        _names.Add(0x2ec0e736, "EyeMakeupColor3");

        _names.Add(0x1a081a93, "CheekMakeupColor1");
        _names.Add(0x4bb0092c, "CheekMakeupColor2");
        _names.Add(0x8653e035, "CheekMakeupColor3");

        _names.Add(0x7d86e792, "LipMakeupColor1");
        _names.Add(0x2c3ef42d, "LipMakeupColor2");
        _names.Add(0xe1dd1d34, "LipMakeupColor3");

        _names.Add(0x442a34ac, "EyeColor");

        _names.Add(0x15e90814, "HairDyeColor1");
        _names.Add(0xa2c7c909, "HairDyeColor2");
    }
}