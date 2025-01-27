using System.Diagnostics;
using StarBreaker.Common;

namespace StarBreaker.Chf;

[DebuggerDisplay("{Name}")]
public class ItemPort
{
    public required uint Key { get; init; }
    public required string? Name { get; init; }
    public required CigGuid Id { get; init; }
    public required ItemPort[] Children { get; init; }

    public static ItemPort Read(ref SpanReader reader)
    {
        var key = reader.Read<uint>();
        var name = ItemPortKeys.TryGetKey(key, out var n) ? n : null;
        if (name == null)
            Console.WriteLine($"Unknown ItemPort key: {key:X8}");
        var id = reader.Read<CigGuid>();
        var childCount = reader.ReadUInt32();
        var anotherCount = reader.ReadUInt32(); //what is this

        var children = new ItemPort[childCount];
        for (var i = 0; i < (int)childCount; i++)
        {
            children[i] = Read(ref reader);
        }

        return new ItemPort
        {
            Key = key,
            Name = name,
            Id = id,
            Children = children
        };
    }

    public int TotalChildren() => Children.Length + Children.Sum(x => x.TotalChildren());
}