using StarBreaker.Common;

namespace StarBreaker.Chf;

public sealed class ScalpChunk 
{
    public static readonly CigGuid Scalp = new("71268392-61f4-40f2-9280-92971478a719");
    public static readonly uint Key = 0xddfa667b;
    
    public required ulong ChildCount { get; init; }
    
    public static ScalpChunk Read(ref SpanReader reader)
    {
        reader.Expect(Key);
        reader.Expect(Scalp);
        reader.Expect(0);
        
        var childCount = reader.Read<uint>();
        switch (childCount)
        {
            case 0:
                return new ScalpChunk { ChildCount = childCount };
            case 2:
            case 3:
            case 4:
            case 5:
            case 6:
            case 7:
                reader.Expect<uint>(5);
                return new ScalpChunk { ChildCount = childCount };
            default:
                throw new Exception("EyelashChunk child count has unexpected value " + childCount);
        }
    }
}