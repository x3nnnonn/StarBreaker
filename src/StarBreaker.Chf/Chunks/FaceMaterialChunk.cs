using StarBreaker.Common;

namespace StarBreaker.Chf;

public sealed class FaceMaterialChunk
{
    //oddity: when the head material is f11, 05-8A-37-A5 is the key.
    //in *all* other cases, 8E-9E-12-72 is the key. The data seems? to be the same.
    //female14_t2_head_material
    public static readonly uint[] Keys =
    [
        ItemPortKeys.GetUIntKey("shader_head"),
        ItemPortKeys.GetUIntKey("shader_Head"),
        ItemPortKeys.GetUIntKey("female23"),
        ItemPortKeys.GetUIntKey("female27"),
    ];

    public required MakeupChunk[] Children { get; init; }
    public required FaceInfoChunk Values { get; init; }
    public required FaceColorsChunk Colors { get; init; }

    public static FaceMaterialChunk Read(ref SpanReader reader)
    {
        reader.ExpectAny<uint>(Keys);
        var childCount = reader.Read<uint>();
        var children = new MakeupChunk[childCount];

        for (var i = 0; i < childCount; i++)
        {
            children[i] = MakeupChunk.Read(ref reader);
        }

        var floats = FaceInfoChunk.Read(ref reader);
        var colors = FaceColorsChunk.Read(ref reader);
        reader.Expect<uint>(5);

        return new FaceMaterialChunk
        {
            Children = children,
            Values = floats,
            Colors = colors
        };
    }
}

// 0x8A5B66DB [f_limbs_m]
// 0x0F04F20D [OBJECTS/SPACESHIPS/SHIPS/CRUS/STARLIFTER/EXTERIOR/CRUS_STARLIFTER_EXT_DOOR_RIGHT_SM_HACK]
// 0xD82F6EE8 [Makeup1SmoothnessB]
// 0x6958D171 [piercings_l_ear_itemport]
// 0x21E79105 [Makeup2OffsetV]
// 0x9EF4EB54 [protos_human_female_face_t1_pu]
// 0x62FBF0AF [BaseMelaninRedness]
// 0x13601A95 [hair_itemport]
// 0x01C113C7 [stubble_itemport]
// 0x0526ED02 [Makeup2MetalnessR]
// 0xBE4C06A4 [Makeup2SmoothnessG]
// 0x8C9E711C [shader_eyeinner]
// 0xE59EBF06 [piercings_mouth_itemport]
// 0x3CB379F2 [Makeup2NumTilesU]
// 0xCFC41264 [SunSpotsOpacity]
// 0x77F57018 [Makeup1NumTilesV]
// 0xC3370BD9 [DyeFadeout]
// 0x15782A6D [Makeup2MetalnessB]
// 0x316B6E4C [f_torso_m]
// 0x68DBEC22 [Makeup3OffsetV]
// 0xA87A7C66 [female23]
// 0xF7E50257 [Makeup3NumTilesU]
// 0xE87727E2 [FrecklesAmount]
// 0x9736C44B [shader_eyeInner]
// 0xB9FA00A3 [BaseMelanin]
// 0x027EB674 [DyeShift]
// 0x7B8B1FD6 [Makeup3OffsetU]
// 0x47010DB9 [head_itemport]
// 0xA59AA7C8 [BaseMelaninVariation]
// 0x589DDCF4 [Makeup3Opacity]
// 0x4AF6C15A [DyeAmount]
// 0xEC67C07D [TattooHueRotation]
// 0x06084076 [DyePigmentVariation]
// 0xB7F8C9B0 [Makeup3MetalnessG]
// 0xC5BB5550 [eyes_itemport]
// 0xBACCC688 [Makeup3SmoothnessB]
// 0x64A583EC [Makeup1NumTilesU]
// 0x9BE3D5D7 [Makeup2SmoothnessR]
// 0x8BBD12B8 [Makeup2SmoothnessB]
// 0x45FBEF91 [piercings_nose_itemport]
// 0x8F3DD294 [Makeup3SmoothnessG]
// 0x73C979A9 [body_m]
// 0xDD6C67F6 [protos_human_male_face_t1_pu]
// 0x20893E71 [Makeup2MetalnessG]
// 0xE9F3E598 [Makeup1OffsetU]
// 0xCAE526BA [Makeup1Opacity]
// 0x32B762F1 [Makeup2OffsetU]
// 0x72129E8E [shader_head]
// 0x1787EE22 [eyebrow_itemport]
// 0xC871A987 [Makeup1SmoothnessR]
// 0x8209DDAC [Makeup3MetalnessB]
// 0xAA9201E7 [Makeup3SmoothnessR]
// 0x11A1A1D3 [Makeup2Opacity]
// 0x554AD20F [SunSpotsAmount]
// 0x190B04E2 [eyelashes_itemport]
// 0xFCD09394 [dna matrix 1.0]
// 0x9CF750C3 [Makeup1MetalnessG]
// 0xEDDE7AF4 [Makeup1SmoothnessG]
// 0xA41FA12C [limbs_m]
// 0x92571AC3 [Makeup3MetalnessR]
// 0x064C1127 [TattooAge]
// 0xE4B5F1A3 [Makeup3NumTilesV]
// 0xA5378A05 [shader_Head]
// 0x98EFBB1C [beard_itemport]
// 0xE7809D46 [material_variant]
// 0xFAA3166C [Makeup1OffsetV]
// 0x9361CB58 [FrecklesOpacity]
// 0xAB6341AC [body_itemport]
// 0xB95883B0 [Makeup1MetalnessR]
// 0xA90644DF [Makeup1MetalnessB]
// 0x2FE38A06 [Makeup2NumTilesV]
