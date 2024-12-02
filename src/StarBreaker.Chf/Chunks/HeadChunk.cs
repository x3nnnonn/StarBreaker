using StarBreaker.Common;

namespace StarBreaker.Chf;

//libs/foundry/records/entities/scitem/characters/human/head/npc/face/pu_protos_head.xml
public sealed class HeadChunk
{
    public static readonly CigGuid Head = new("1d5cfab3-bf80-4550-b4ab-39e896a7086e");
    public static readonly uint Key = ItemPortKeys.GetUIntKey("head_itemport");

    public required ulong ChildCount { get; init; }
    public required EyesChunk? Eyes { get; init; }
    public required HairChunk? Hair { get; init; }
    public required EyebrowChunk? Eyebrow { get; init; }
    public required EyelashChunk? Eyelash { get; init; }
    public required FacialHairChunk? FacialHair { get; init; }
    public required List<PiercingChunk> Piercings { get; init; }

    public static HeadChunk Read(ref SpanReader reader)
    {
        reader.Expect(Key);
        reader.Expect(Head);

        var childCount = reader.Read<ulong>();

        EyesChunk? eyes = null;
        HairChunk? hair = null;
        EyebrowChunk? eyebrow = null;
        EyelashChunk? eyelash = null;
        FacialHairChunk? facialHair = null;
        List<PiercingChunk> piercings = new();
        ScalpChunk? scalp = null;
        PiercingsEyebrowsItemport? piercingEyebrows = null;
        
        for (var i = 0; i < (int)childCount; i++)
        {
            var k = reader.Peek<uint>();
            if (k == EyesChunk.Key)
            {
                eyes = EyesChunk.Read(ref reader);
            }
            else if (k == HairChunk.Key)
            {
                hair = HairChunk.Read(ref reader);
            }
            else if (k == EyebrowChunk.Key)
            {
                eyebrow = EyebrowChunk.Read(ref reader);
            }
            else if (k == EyelashChunk.Key)
            {
                eyelash = EyelashChunk.Read(ref reader);
            }
            else if (k == FacialHairChunk.Key)
            {
                facialHair = FacialHairChunk.Read(ref reader);
            }
            else if (k == ScalpChunk.Key)
            {
                scalp = ScalpChunk.Read(ref reader);
            }
            else if (k == PiercingsEyebrowsItemport.Key)
            {
                piercingEyebrows = PiercingsEyebrowsItemport.Read(ref reader);
            }
            else if (PiercingChunk.Keys.Contains(k))
            {
                piercings.Add(PiercingChunk.Read(ref reader));
            }
            else
            {
                throw new Exception("Unknown HeadChunk child chunk");
            }
        }

        return new HeadChunk
        {
            ChildCount = childCount,
            Eyes = eyes ?? throw new Exception("EyesProperty is required"),
            Eyelash = eyelash ?? throw new Exception("EyelashProperty is required"),
            Hair = hair ?? new HairChunk { HairType = HairType.None, Modifier = null },
            Eyebrow = eyebrow ?? new EyebrowChunk { EyebrowType = EyebrowType.None, ChildCount = 0 },
            FacialHair = facialHair ?? new FacialHairChunk { FacialHairType = FacialHairType.None, Modifier = null },
            Piercings = piercings
        };
    }
}