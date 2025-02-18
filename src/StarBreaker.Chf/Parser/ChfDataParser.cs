using StarBreaker.Common;

namespace StarBreaker.Chf.Parser;

public class ChfDataParser
{
    public required CigGuid GenderId { get; init; }
    public required DnaChunk Dna { get; init; }
    public required ItemPort ItemPort { get; init; }
    public required List<Material> Materials { get; init; }
    
    public static ChfDataParser FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new SpanReader(data);

        reader.Expect<uint>(2);
        reader.Expect<uint>(7);
        var gender = reader.Read<CigGuid>();
        reader.Expect(CigGuid.Empty);
        var dna = DnaChunk.Read(ref reader);
        var itemPortCount = reader.ReadUInt64();
        var itemPort = ItemPort.Read(ref reader);
        reader.Expect<uint>(5);
        var materials = new List<Material>();
        while (reader.Remaining > 0)
            materials.Add(Material.Read(ref reader));

        return new ChfDataParser
        {
            GenderId = gender,
            Dna = dna,
            ItemPort = itemPort,
            Materials = materials
        };
    }
}