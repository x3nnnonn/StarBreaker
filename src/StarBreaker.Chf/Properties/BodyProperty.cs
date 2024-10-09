
using StarBreaker.Common;

namespace StarBreaker.Chf;

//entities/scitem/characters/human.body/body_01_noMagicPocket
public sealed class BodyProperty
{
    public static readonly CigGuid Body = new("dbaa8a7d-755f-4104-8b24-7b58fd1e76f6");

    public const uint Key = 0xAB6341AC;
    public required HeadProperty Head { get; init; }
    
    public static BodyProperty Read(ref SpanReader reader)
    {
        reader.Expect(Key);
        reader.Expect(Body);
        reader.Expect<ulong>(1);
        
        var head = HeadProperty.Read(ref reader);

        return new BodyProperty
        {
            Head = head,
        };
    }
    
}