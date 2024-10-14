using StarBreaker.Common;

namespace StarBreaker.Chf;

public sealed class PiercingChunk
{
    //piercing\pcg_ball_l_ear_07.xml
    //piercing\pcg_ball_nostril_04.xml
    //piercing\pcg_ball_mouth_04.xml
    
    //?? | ?? | 
    public static readonly uint[] Keys = [0x6958D171, 0x45FBEF91, 0xE59EBF06];
    
    public CigGuid Guid { get; init; }
    
    public static PiercingChunk Read(ref SpanReader reader)
    {
        reader.ExpectAny<uint>(Keys);
        var guid = reader.Read<CigGuid>();
        reader.Expect(0);
        var count = reader.Read<uint>();
        
        // idk
        if (count is 7 or 6)
        {
            reader.Expect(5);
        }
        
        return new PiercingChunk
        {
            Guid = guid
        };
    }
}