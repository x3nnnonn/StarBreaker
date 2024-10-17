using StarBreaker.Common;

namespace StarBreaker.Chf;

public sealed class PiercingChunk
{
    //piercings_l_ear_itemport
    //piercings_nose_itemport
    //piercings_mouth_itemport
    //piercings_r_ear_itemport
    public static readonly uint[] Keys =
    [
        0x6958D171,
        0x45FBEF91,
        0xE59EBF06,
        0x6D6DE693
    ];

    public CigGuid Guid { get; init; }

    public static PiercingChunk Read(ref SpanReader reader)
    {
        reader.ExpectAnyKey([
            "piercings_l_ear_itemport",
            "piercings_nose_itemport",
            "piercings_mouth_itemport",
            "piercings_r_ear_itemport"
        ]);
        var guid = reader.Read<CigGuid>();
        reader.Expect(0);
        var count = reader.Read<uint>();

        // idk
        if (count is 7 or 6)
        {
            reader.Expect(5);
        }

        return new PiercingChunk
        {
            Guid = guid
        };
    }
}