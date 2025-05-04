using StarBreaker.Common;

namespace StarBreaker.CryChunkFile;

public class PhysicalHierarchyChunk : IChunk
{
    public static IChunk Read(ref SpanReader reader)
    {
        // //var uints = reader.ReadSpan<uint>(44);
        // var numSomething = reader.ReadUInt32();
        // //2179497591 0x81E87E77
        // //2171208412 0x816A02DC
        // //3951608477 0xEB88C29D
        // var someUint = reader.ReadUInt32();
        // reader.Expect<uint>(1);
        // reader.Expect<uint>(0xffffffff);
        // reader.Expect<uint>(0);
        // reader.Expect<uint>(0);
        // reader.Expect<uint>(0);
        //TODO
        
        
        return new PhysicalHierarchyChunk();
    }
}