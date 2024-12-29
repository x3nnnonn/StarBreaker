using System.Diagnostics;
using StarBreaker.Common;

namespace StarBreaker.Chf;

public sealed class DnaChunk
{
    private const int Size = 0xD8;
    private const int PartCount = 48;

    public required string DnaString { get; init; }
    public required uint ChildCount { get; init; }
    public required Dictionary<FacePart, DnaPart[]> Parts { get; init; }

    public static DnaChunk Read(ref SpanReader reader)
    {
        reader.Expect<ulong>(Size);
        var dnaBytes = reader.ReadBytes(Size).ToArray();
        var dnaString = Convert.ToHexString(dnaBytes);

        var childReader = new SpanReader(dnaBytes);

        childReader.ExpectKey("dna matrix 1.0");
        childReader.ExpectAnyKey(["protos_human_male_face_t1_pu", "protos_human_female_face_t1_pu"]);
        childReader.ExpectAny([
            0x65E740D3, 0x65D75204, 0x66EBFAD1, 0x66DF165F,
            0x674986D1, 0x67448F99
        ]);
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

        return new DnaChunk
        {
            DnaString = dnaString,
            ChildCount = maxHeadId,
            Parts = perBodyPart
        };
    }
}

[DebuggerDisplay("{HeadId} {Percent}")]
public sealed class DnaPart
{
    public required byte HeadId { get; init; }
    public required float Percent { get; init; }

    public static DnaPart Read(ref SpanReader reader)
    {
        var value = reader.Read<ushort>();
        var headId = reader.Read<byte>();
        reader.Expect<byte>(0);

        return new DnaPart
        {
            Percent = value / (float)ushort.MaxValue * 100f,
            HeadId = headId
        };
    }
}

public enum FacePart
{
    EyebrowLeft = 0,
    EyebrowRight = 1,
    EyeLeft = 2,
    EyeRight = 3,
    Nose = 4,
    EarLeft = 5,
    EarRight = 6,
    CheekLeft = 7,
    CheekRight = 8,
    Mouth = 9,
    Jaw = 10,
    Crown = 11
}