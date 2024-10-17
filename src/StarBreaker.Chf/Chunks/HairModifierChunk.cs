using StarBreaker.Common;

namespace StarBreaker.Chf;

//libs/foundry/records/entities/scitem/characters/human/appearance_modifier/hair_variant/hair_var_brown.xml
public sealed class HairModifierChunk
{
    public static readonly uint Key = ItemPortKeys.GetUIntKey("material_variant");
    public required ulong ChildCount { get; init; }

    public static HairModifierChunk Read(ref SpanReader reader)
    {
        var bruh = ItemPortKeys.GetUIntKey("material_variant");
        var anotehrBruh = Crc32c.FromString("material_variant");
        var anotehrBruh2 = Crc32c.FromString("Material_variant");
        var anotehrBruh3 = Crc32c.FromString("Stubble_Itemport_Skinitemport");
        //stubble_itemport_skinitemport
        reader.ExpectAnyKey([
            "material_variant",
            "stubble_itemport"
        ]);
        var guid = reader.Read<CigGuid>();
        if (guid != HairVarBrown) throw new Exception("Unexpected Hair modifier guid: " + guid);

        reader.Expect(0);
        var count = reader.Read<uint>(); //0 for hair modifier, 6 for facial hair modifier

        switch (count)
        {
            case 0:
                return new HairModifierChunk { ChildCount = count };
            case 6:
                //the data i have has this 5 here but the next property is right after.
                //Unknown what this is.
                //count of how many objects are left to read??
                reader.Expect(5);
                return new HairModifierChunk { ChildCount = count };
            default:
                throw new Exception("HairModifierChunk child count is not 0 or 6");
        }
    }

    public static readonly CigGuid HairVarBrown = new("12ce4ce5-e49a-4dab-9d31-ad262faaddf2");
}