using StarBreaker.Common;

namespace StarBreaker.Chf;

public sealed class PiercingChunk
{
    //piercing\pcg_ball_l_ear_07.xml
    //piercing\pcg_ball_nostril_04.xml
    //piercing\pcg_ball_mouth_04.xml
    public static readonly uint[] Keys = [0x6958D171,0x45FBEF91,0xE59EBF06];
    
    public CigGuid Guid { get; init; }
    
    public static PiercingChunk Read(ref SpanReader reader)
    {
        reader.ExpectAny<uint>(Keys);
        var guid = reader.Read<CigGuid>();
        reader.Expect(0);
        if (reader.Peek<uint>() == 7)
        {
            reader.Expect(7);
            reader.Expect(5);
        }
        else
        {
            reader.Expect(0);
        }

        return new PiercingChunk()
        {
            Guid = guid
        };
    }
}