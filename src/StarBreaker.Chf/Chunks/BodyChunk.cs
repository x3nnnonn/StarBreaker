using StarBreaker.Common;

namespace StarBreaker.Chf;

//entities/scitem/characters/human.body/body_01_noMagicPocket
public sealed class BodyChunk
{
    public static readonly CigGuid Body = new("dbaa8a7d-755f-4104-8b24-7b58fd1e76f6");

    public static readonly uint Key = ItemPortKeys.GetUIntKey("body_itemport");
    public required HeadChunk Head { get; init; }

    public static BodyChunk Read(ref SpanReader reader)
    {
        reader.Expect(Key);
        reader.Expect(Body);
        var childCount = reader.Read<ulong>();

        if (childCount != 1)
            throw new Exception("BodyChunk child count is not 1");

        var head = HeadChunk.Read(ref reader);

        return new BodyChunk
        {
            Head = head,
        };
    }
}