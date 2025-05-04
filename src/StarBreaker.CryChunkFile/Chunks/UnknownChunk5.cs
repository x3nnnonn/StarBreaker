using StarBreaker.Common;

namespace StarBreaker.CryChunkFile;

internal class UnknownChunk5 : IChunk
{
    public required Unknown5Child[] Children { get; init; }

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
}

internal class Unknown5Child
{
    public required string Name { get; init; }
    public required byte[] Data { get; init; }
    public required uint Flag { get; init; }

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