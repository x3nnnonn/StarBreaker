using StarBreaker.Common;

namespace StarBreaker.CryChunkFile;

internal class UnknownChunk5 : IChunk
{
    public Unknown5Child[] Children { get; private set; }
    
    public static IChunk Read(ref SpanReader reader)
    {
        var count = reader.ReadUInt32();
        
        var arr = new Unknown5Child[count];
        
        for (var i = 0; i < count; i++)
        {
            arr[i] = Unknown5Child.Read(ref reader);
        }
        
        return new UnknownChunk5
        {
            Children = arr
        };
    }

    public void WriteXmlTo(TextWriter writer)
    {
        throw new NotImplementedException();
    }

    internal class Unknown5Child
    {
        public string Name { get; private set; }
        public byte[] Data { get; private set; }
        public uint Flag { get; private set; }
        
        public static Unknown5Child Read(ref SpanReader reader)
        {
            var flag = reader.ReadUInt32();
            var name = reader.ReadStringInsideArray(256);
            var data = reader.ReadBytes(48);
            
            return new Unknown5Child
            {
                Flag = flag,
                Name = name,
                Data = data.ToArray()
            };
        }
    }
}