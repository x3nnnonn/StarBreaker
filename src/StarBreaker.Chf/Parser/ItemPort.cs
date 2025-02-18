using System.Diagnostics;
using StarBreaker.Common;

namespace StarBreaker.Chf.Parser;

[DebuggerDisplay("{Name}")]
public class ItemPort
{
    public required NameHash Name { get; init; }
    public required CigGuid Id { get; init; }
    public required ItemPort[] Children { get; init; }

    public static ItemPort Read(ref SpanReader reader)
    {
        var name = reader.Read<NameHash>();
        var id = reader.Read<CigGuid>();
        var childCount = reader.ReadUInt32();
        var anotherCount = reader.ReadUInt32(); //what is this

        var children = new ItemPort[childCount];
        for (var i = 0; i < (int)childCount; i++)
            children[i] = Read(ref reader);

        return new ItemPort
        {
            Name = name,
            Id = id,
            Children = children
        };
    }

    public int TotalChildren() => Children.Length + Children.Sum(x => x.TotalChildren());
}