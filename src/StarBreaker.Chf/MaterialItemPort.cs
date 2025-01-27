using System.Diagnostics;
using StarBreaker.Common;

namespace StarBreaker.Chf;

[DebuggerDisplay("{Name}")]
public class MaterialItemPort
{
    public required uint Key { get; init; }
    public required string Name { get; init; }
    public required CigGuid Id { get; init; }
    public required ItemPort[] Children { get; init; }

    public static MaterialItemPort Read(ref SpanReader reader)
    {
        var key = reader.Read<uint>();
        var name = ItemPortKeys.TryGetKey(key, out var n) ? n : "";
        if (name == "")
            Console.WriteLine($"Unknown ItemPort key: {key:X8}");
        var id = reader.Read<CigGuid>();
        var param = reader.ReadUInt32();
        reader.Expect(CigGuid.Empty); //???

        var childreCount = reader.ReadUInt32();
        var anotherCount = reader.ReadUInt32(); //what is this

        var children = new MaterialItemPort[childreCount];
        Console.WriteLine($"MaterialItemPort childCount: {childreCount}, anotherCount: {anotherCount}, next key: {reader.Peek<uint>():X8}");

        //TODO: wrong
        for (int i = 0; i < anotherCount; i++)
        {
            children[i] = MaterialItemPort.Read(ref reader);
        }

        return new MaterialItemPort
        {
            Key = key,
            Name = name,
            Id = id,
            Children = []
        };
    }

    public int TotalChildren() => Children.Length + Children.Sum(x => x.TotalChildren());
}