using System;
using StarBreaker.Common;

namespace StarBreaker.Chf;

//libs/foundry/records/entities/scitem/characters/human/head/pu/eyes/pu_head_eyes_white_charactercustomizer.xml
public sealed class EyesChunk
{
    public static readonly CigGuid Eyes = new("6b4ca363-e160-4871-b709-e47467b40310");
    public static readonly uint Key = ItemPortKeys.GetUIntKey("eyes_itemport");
    
    public static EyesChunk Read(ref SpanReader reader)
    {
        reader.Expect(Key);
        reader.Expect(Eyes);
        reader.Expect<ulong>(0);

        return new EyesChunk();
    }
}