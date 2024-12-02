using StarBreaker.Common;

namespace StarBreaker.Chf;

public sealed class PiercingsEyebrowsItemport 
{
    public static readonly uint Key = ItemPortKeys.GetUIntKey("piercings_eyebrows_itemport");
    
    public required CigGuid Id { get; init; }
    public required ulong ChildCount { get; init; }
    
    public static PiercingsEyebrowsItemport Read(ref SpanReader reader)
    {
        reader.Expect(Key);
        var guid = reader.Read<CigGuid>();
        
        reader.Expect(0);
        
        var childCount = reader.Read<uint>();
        switch (childCount)
        {
            case 0:
                return new PiercingsEyebrowsItemport { ChildCount = childCount, Id = guid };
            case 2:
            case 3:
            case 4:
            case 5:
            case 6:
            case 7:
                reader.Expect<uint>(5);
                return new PiercingsEyebrowsItemport { ChildCount = childCount, Id = guid };
            default:
                throw new Exception("EyelashChunk child count has unexpected value " + childCount);
        }
    }
    
    //todo cant be bothered, ctrl+f in the datacore and all of them are there
    public static readonly CigGuid PiercingStudEyebrow04 = new("2fdc6c23-c36a-489d-b418-45d1d5785b40");
    public static readonly CigGuid PiercingStudEyebrow06 = new("345e4b4d-2350-42cf-bef8-c301af3a4399");
    public static readonly CigGuid PiercingBallEyebrow07 = new("0d317b0f-8771-4e56-8c6b-d09f39e0d4a8");
}