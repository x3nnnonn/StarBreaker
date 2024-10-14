using System.Diagnostics;
using StarBreaker.Common;

namespace StarBreaker.Chf;

[DebuggerDisplay("{Name}")]
public class MaterialItemPort
{
    public uint Key { get; init; }
    public string Name { get; init; }
    public CigGuid Id { get; init; }
    public ItemPort[] Children { get; init; }

    public static MaterialItemPort Read(ref SpanReader reader)
    {
        var key = reader.Read<uint>();
        var name = ItemPortKeys.TryGetKey(key, out var n) ? n : "";
        if (name == "")
            Console.WriteLine($"Unknown ItemPort key: {key:X8}");
        var id = reader.Read<CigGuid>();
        var param = reader.ReadUInt32();
        reader.Expect(CigGuid.Empty);//???
        
        var childreCount = reader.ReadUInt32();
        var anotherCount = reader.ReadUInt32(); //what is this

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