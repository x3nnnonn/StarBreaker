using StarBreaker.Common;

namespace StarBreaker.Chf;

public sealed class Dna
{
    private const int Size = 0xD8;
    private const int PartCount = 48;

    public required string DnaString { get; init; }
    public required uint MaxHeadId { get; init; }
    public required Dictionary<FacePart, DnaPart[]> Parts { get; init; }

    public static Dna Read(ref SpanReader reader)
    {
        reader.Expect<ulong>(Size);
        var dnaBytes = reader.ReadBytes(Size).ToArray();
        var dnaString = Convert.ToHexString(dnaBytes);

        var childReader = new SpanReader(dnaBytes);

        childReader.Expect(NameHash.FromString("dna matrix 1.0"));
        var key = childReader.Read<NameHash>();
        if (key == NameHash.FromString("protos_human_male_face_t1_pu"))
        {
            childReader.ExpectAny([
                0x65e740d3,
                0x66df165f,
                0x674986d1,
            ]);
        }
        else if (key == NameHash.FromString("protos_human_female_face_t1_pu"))
        {
            childReader.ExpectAny([
                0x65d75204,
                0x66ebfad1,
                0x67448f99,
            ]);
        }
        else
        {
            throw new Exception("Invalid DNA");
        }

        childReader.Expect<uint>(0);
        childReader.Expect<ushort>(12); //number of parts?
        childReader.Expect<ushort>(4); //blend per part?
        childReader.Expect<ushort>(4); //unknown
        
        var maxHeadId = childReader.Read<ushort>();

        var perBodyPart = Enum.GetValues<FacePart>().ToDictionary(x => x, _ => new DnaPart[4]);
        for (var i = 0; i < PartCount; i++)
        {
            perBodyPart[(FacePart)(i % 12)][i / 12] = DnaPart.Read(ref childReader);
        }

        foreach (var (k, v) in perBodyPart)
        {
            if (Math.Abs(v.Sum(x => x.Percent) - 100) > 5f) //it's fiiiine
                throw new Exception($"Invalid part percent for {k}");
        }

        return new Dna
        {
            DnaString = dnaString,
            MaxHeadId = maxHeadId,
            Parts = perBodyPart
        };
    }
}