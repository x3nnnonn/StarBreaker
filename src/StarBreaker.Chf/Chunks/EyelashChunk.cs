
using StarBreaker.Common;

namespace StarBreaker.Chf;

//libs/foundry/records/entities/scitem/characters/human/head/shared/eyelashes/head_eyelashes.xml
public sealed class EyelashChunk 
{
    public static readonly CigGuid Eyelashes = new("6217c113-a448-443b-82aa-1bb108ba8e11");
    public const uint Key = 0x190b04e2;
    
    public required ulong ChildCount { get; init; }
    
    public static EyelashChunk Read(ref SpanReader reader)
    {
        reader.Expect(Key);
        reader.Expect(Eyelashes);
        reader.Expect(0);
        
        var childCount = reader.Read<uint>();
        switch (childCount)
        {
            case 0:
                return new EyelashChunk { ChildCount = childCount };
            case 2:
            case 3:
            case 4:
            case 5:
            case 6:
            case 7:
                reader.Expect<uint>(5);
                return new EyelashChunk { ChildCount = childCount };
            default:
                throw new Exception("EyelashChunk child count has unexpected value " + childCount);
        }
    }
}