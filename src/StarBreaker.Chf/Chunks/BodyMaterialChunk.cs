using StarBreaker.Common;

namespace StarBreaker.Chf;

public sealed class BodyMaterialChunk
{
    public static readonly CigGuid m_body_character_customizer = new("fa5042a3-8568-48f5-bf36-02dc98191b2d");
    public static readonly CigGuid f_body_character_customizer = new("f0153262-588d-4ae8-8c06-53bf98cf80a5");
    
    public const uint Key = 0x27424D58;
    
    public required uint AdditionalParams { get; init; }
    public required Color TorsoColor { get; init; }
    public required Color LimbColor { get; init; }
    
    public static BodyMaterialChunk Read(ref SpanReader reader)
    {
        reader.Expect(Key);
        var guid = reader.Read<CigGuid>();
        var isMan = guid switch
        {
            _ when guid == f_body_character_customizer => false,
            _ when guid == m_body_character_customizer => true,
            _ => true,//if we do this it seems to work correctly in the next checks. Unsure.
        };
        if (guid == CigGuid.Empty)
        {
            Console.WriteLine("[WARN] Empty guid in BodyMaterialChunk");
        }

        var additionalParams = reader.Read<uint>();
        reader.Expect<uint>(0);
        reader.Expect<uint>(0);
        reader.Expect<uint>(0);
        reader.Expect<uint>(0);
        reader.Expect<uint>(2);
        reader.Expect<uint>(5);
        reader.Expect(isMan ? 0x73C979A9 : 0x316B6E4C);
        reader.Expect<uint>(0);
        reader.Expect<uint>(0);
        reader.Expect<uint>(0);
        reader.Expect<uint>(1);
        reader.Expect<uint>(0);
        var c1 = reader.ReadKeyedValue<Color>(0xbd530797);
        reader.Expect<uint>(5);
        reader.Expect(isMan ? 0xA41FA12C : 0x8A5B66DB);
        reader.Expect<uint>(0);
        reader.Expect<uint>(0);
        reader.Expect<uint>(0);
        reader.Expect<uint>(1);
        reader.Expect<uint>(0);
        var c2 = reader.ReadKeyedValue<Color>(0xbd530797);
        //todo: why
        if (reader.Remaining >= 4)
            reader.Expect<uint>(5);
        
        return new BodyMaterialChunk
        {
            AdditionalParams = additionalParams,
            TorsoColor = c1,
            LimbColor = c2
        };
    }
}