using System.Diagnostics;

namespace StarBreaker.DataCore;

[DebuggerDisplay("{_name}")]
public sealed class XmlNode
{
    public readonly string _name;
    public readonly List<XmlNode> _children;
    public readonly List<XmlAttribute> _attributes;

    public XmlNode(string name)
    {
        _name = name;
        _children = [];
        _attributes = [];
    }

    public void AppendChild(XmlNode child) => _children.Add(child);

    public void AppendAttribute(XmlAttribute xmlAttribute) => _attributes.Add(xmlAttribute);

    public void WriteTo(TextWriter writer)
    {
        WriteToInternal(writer, 0, new Stack<XmlNode>());
    }

    public void WriteToInternal(TextWriter writer, int depth, Stack<XmlNode> stack)
    {
        stack.Push(this);

        for (var i = 0; i < depth; i++)
        {
            writer.Write("  ");
        }

        writer.Write('<');
        writer.Write(_name);

        foreach (var attribute in _attributes)
        {
            writer.Write(' ');
            attribute.WriteTo(writer);
        }

        if (_children.Count == 0)
        {
            writer.Write("/>");
            return;
        }

        writer.Write('>');

        foreach (var child in _children)
        {
            if (stack.Contains(child))
            {
                writer.WriteLine();
                for (var i = 0; i < depth; i++)
                {
                    writer.Write("  ");
                }

                writer.Write("<CircularReference />");
                writer.WriteLine();
                for (var i = 0; i < depth; i++)
                {
                    writer.Write("  ");
                }

                writer.Write("</");
                writer.Write(_name);
                writer.Write('>');
                return;
            }

            writer.WriteLine();
            child.WriteToInternal(writer, depth + 1, stack);
        }

        writer.WriteLine();

        for (var i = 0; i < depth; i++)
        {
            writer.Write("  ");
        }

        writer.Write("</");
        writer.Write(_name);
        writer.Write('>');

        stack.Pop();
    }
}