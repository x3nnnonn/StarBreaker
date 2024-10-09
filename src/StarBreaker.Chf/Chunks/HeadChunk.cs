
using StarBreaker.Common;

namespace StarBreaker.Chf;

//libs/foundry/records/entities/scitem/characters/human/head/npc/face/pu_protos_head.xml
public sealed class HeadChunk
{
    public static readonly CigGuid Head = new("1d5cfab3-bf80-4550-b4ab-39e896a7086e");
    public const uint Key = 0x47010DB9;
    
    public required ulong ChildCount { get; init; }
    public required EyesChunk? Eyes { get; init; }
    public required HairChunk? Hair { get; init; }
    public required EyebrowChunk? Eyebrow { get; init; }
    public required EyelashChunk? Eyelash { get; init; }
    public required FacialHairChunk? FacialHair { get; init; }
    
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
        
        for (var i = 0; i < (int)childCount; i++)
        {
            switch (reader.Peek<uint>())
            {
                case EyesChunk.Key:
                    eyes = EyesChunk.Read(ref reader);
                    break;
                case HairChunk.Key:
                    hair = HairChunk.Read(ref reader);
                    break;
                case EyebrowChunk.Key:
                    eyebrow = EyebrowChunk.Read(ref reader);
                    break;
                case EyelashChunk.Key:
                    eyelash = EyelashChunk.Read(ref reader);
                    break;
                case FacialHairChunk.Key:
                    facialHair = FacialHairChunk.Read(ref reader);
                    break;
                case 0x6958D171:
                {
                    //todo: what is this?
                    reader.Expect(0x6958D171);
                    var guid = reader.Read<CigGuid>();
                    reader.Expect(0);
                    reader.Expect(0);
                    Console.WriteLine("New Chunk with key 0x6958D171. Guid: " + guid);
                    break;
                }
                case 0x45FBEF91:
                {
                    //todo: what is this?
                    reader.Expect(0x45FBEF91);
                    var guid = reader.Read<CigGuid>();
                    reader.Expect(0);
                    reader.Expect(7);
                    reader.Expect(5);
                    Console.WriteLine("New Chunk with key 0x45FBEF91. Guid: " + guid);
                    break;
                }
                case 0xE59EBF06:
                {
                    //todo: what is this?
                    reader.Expect(0xE59EBF06);
                    var guid = reader.Read<CigGuid>();
                    reader.Expect(0);
                    reader.Expect(7);
                    reader.Expect(5);
                    Console.WriteLine("New Chunk with key 0xE59EBF06. Guid: " + guid);
                    break;
                }
                default:
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
            FacialHair = facialHair ?? new FacialHairChunk { FacialHairType = FacialHairType.None, Modifier = null }
        };
    }
}