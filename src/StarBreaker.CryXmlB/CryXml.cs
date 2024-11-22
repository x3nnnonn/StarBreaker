using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using StarBreaker.Common;

namespace StarBreaker.CryXmlB;

public readonly struct CryXml
{
    private static ReadOnlySpan<byte> magic => "CryXmlB\0"u8;
    private const int magicLength = 8;

    private readonly CryXmlNode[] _nodes;
    private readonly int[] _childIndices;
    private readonly CryXmlAttribute[] _attributes;
    private readonly byte[] _stringData;

    public static bool TryOpen(Stream data, out CryXml cryXml)
    {
        var position = data.Position;
        using var br = new BinaryReader(data, Encoding.ASCII, true);
        var thisMagic = br.ReadBytes(magicLength);
        data.Position = position;
        if (!magic.SequenceEqual(thisMagic))
        {
            cryXml = default;
            return false;
        }

        cryXml = new CryXml(data);
        return true;
    }

    public CryXml(Stream stream)
    {
        using var br = new BinaryReader(stream, Encoding.ASCII, true);
        var thisMagic = br.ReadBytes(magicLength);
        if (!magic.SequenceEqual(thisMagic))
            throw new Exception("Invalid CryXmlB file");

        var header = br.Read<CryXmlHeader>();
        _nodes = br.ReadArray<CryXmlNode>((int)header.NodeCount);
        _childIndices = br.ReadArray<int>((int)header.ChildCount);
        _attributes = br.ReadArray<CryXmlAttribute>((int)header.AttributeCount);
        _stringData = br.ReadBytes((int)header.StringDataSize);
    }

    public static bool IsCryXmlB(ReadOnlySpan<byte> data)
    {
        return data.Length > magicLength && data[..magicLength].SequenceEqual(magic);
    }

    public void WriteXml(XmlWriter writer)
    {
        if (_nodes[0].ParentIndex != -1)
            throw new Exception("Root node has parent");

        WriteXmlElement(writer, 0);
    }

    private void WriteXmlElement(XmlWriter writer, int nodeIndex)
    {
        var node = _nodes[nodeIndex];
        writer.WriteStartElement(GetString(_stringData, (int)node.TagStringOffset));

        var thisAttributes = _attributes.AsSpan(node.FirstAttributeIndex, node.AttributeCount);
        foreach (var attribute in thisAttributes)
        {
            writer.WriteAttributeString(
                GetString(_stringData, (int)attribute.KeyStringOffset),
                GetString(_stringData, (int)attribute.ValueStringOffset)
            );
        }

        var thisChildren = _childIndices.AsSpan(node.FirstChildIndex, node.ChildCount);
        foreach (var childIndex in thisChildren)
        {
            WriteXmlElement(writer, childIndex);
        }

        writer.WriteEndElement();
    }

    private static string GetString(Span<byte> data, int offset)
    {
        var relevantData = data[offset..];
        var length = relevantData.IndexOf((byte)'\0');

        if (length == 0)
            return "";

        return Encoding.ASCII.GetString(relevantData[..length]);
    }

    public void WriteXmlFast(TextWriter writer)
    {
        if (_nodes[0].ParentIndex != -1)
            throw new Exception("Root node has parent");

        writer.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        WriteXmlElementFast(writer, 0, 0);
    }

    private void WriteXmlElementFast(TextWriter writer, int depth, int nodeIndex)
    {
        var node = _nodes[nodeIndex];

        for (var i = 0; i < depth; i++)
        {
            writer.Write(' ');
            writer.Write(' ');
        }

        writer.Write('<');
        if (writer.WriteString(_stringData, (int)node.TagStringOffset) == 0)
            writer.Write("__unknown__");

        var attributes = _attributes.AsSpan(node.FirstAttributeIndex, node.AttributeCount);
        foreach (var attribute in attributes)
        {
            writer.Write(' ');
            writer.WriteString(_stringData, (int)attribute.KeyStringOffset);
            writer.Write('=');
            writer.Write('\"');
            writer.WriteString(_stringData, (int)attribute.ValueStringOffset);
            writer.Write('\"');
        }

        var stringElementLength = _stringData.AsSpan((int)node.ItemType).IndexOf((byte)'\0');
        var hasStringElement = stringElementLength != 0;
        var hasChildren = node.ChildCount != 0;

        if (!hasChildren && !hasStringElement)
        {
            writer.Write(' ');
            writer.Write('/');
            writer.WriteLine('>');
            return;
        }

        writer.Write('>');

        if (hasStringElement && !hasChildren)
        {
            writer.WriteString(_stringData, (int)node.ItemType);
            writer.Write('<');
            writer.Write('/');
            writer.WriteString(_stringData, (int)node.TagStringOffset);
            writer.WriteLine('>');
            return;
        }

        if (hasStringElement)
        {
            writer.WriteLine();
            writer.WriteString(_stringData, (int)node.ItemType);
            writer.WriteLine();
        }

        if (!hasStringElement && hasChildren)
        {
            writer.WriteLine();
        }

        var childIndices = _childIndices.AsSpan(node.FirstChildIndex, node.ChildCount);
        foreach (var childIndex in childIndices)
        {
            WriteXmlElementFast(writer, depth + 1, childIndex);
        }

        for (var i = 0; i < depth; i++)
        {
            writer.Write(' ');
            writer.Write(' ');
        }

        writer.Write('<');
        writer.Write('/');
        writer.WriteString(_stringData, (int)node.TagStringOffset);

        if (nodeIndex == 0)
            writer.Write('>');
        else
            writer.WriteLine('>');
    }
}