using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace StarBreaker.Forge;

[DebuggerDisplay("{_name.Id}")]
public sealed class XmlNode
{
    public readonly DataForgeStringId _name;
    public readonly List<XmlNode> _children;
    public readonly List<XmlAttribute> _attributes;
    public XmlNode? _parent;
    
    public XmlNode(DataForgeStringId name)
    {
        _name = name;
        _children = [];
        _attributes =  [];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendChild(XmlNode child)
    {
        child._parent = this;
        _children.Add(child);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendAttribute(XmlAttribute xmlAttribute) => _attributes.Add(xmlAttribute);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteTo(TextWriter writer, int depth, Database database, Dictionary<int, int[]> offsets)
    {
        var name = database.GetString(_name);
        for (var i = 0; i < depth; i++)
        {
            writer.Write("  ");
        }

        writer.Write('<');
        writer.Write(name);

        foreach (var attribute in _attributes)
        {
            writer.Write(' ');
            attribute.WriteTo(writer);
        }
        
        if (_children.Count == 0)
        {
            writer.Write(" />");
            return;
        }

        writer.Write('>');

        foreach (var child in _children)
        {
            writer.WriteLine();
            child.WriteTo(writer, depth + 1, database, offsets);
        }
        
        writer.WriteLine();
        
        for (var i = 0; i < depth; i++)
        {
            writer.Write("  ");
        }

        writer.Write("</");
        writer.Write(name);
        writer.Write('>');
    }
}