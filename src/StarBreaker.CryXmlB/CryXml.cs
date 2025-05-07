using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using System.Xml.Linq;
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

        var header = br.BaseStream.Read<CryXmlHeader>();
        _nodes = br.BaseStream.ReadArray<CryXmlNode>((int)header.NodeCount);
        _childIndices = br.BaseStream.ReadArray<int>((int)header.ChildCount);
        _attributes = br.BaseStream.ReadArray<CryXmlAttribute>((int)header.AttributeCount);
        _stringData = br.BaseStream.ReadArray<byte>((int)header.StringDataSize);
    }

    public static bool IsCryXmlB(ReadOnlySpan<byte> data)
    {
        return data.Length > magicLength && data[..magicLength].SequenceEqual(magic);
    }

    public static bool IsCryXmlB(Stream stream)
    {
        if (stream.Length < magicLength)
            return false;

        var before = stream.Position;
        Span<byte> buffer = stackalloc byte[magicLength];
        stream.ReadExactly(buffer);
        stream.Position = before;
        return buffer.SequenceEqual(magic);
    }

    public void WriteTo(XmlWriter writer)
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
            var key = GetString(_stringData, (int)attribute.KeyStringOffset);
            var val = GetString(_stringData, (int)attribute.ValueStringOffset);

            //these mess things up. unsure if we should make a better attempt at preserving them?
            if (key.StartsWith("xmlns"))
            {
                Debug.WriteLine($"Skipping xmlns attribute {key}={val} for node {nodeIndex}");
                continue;
            }

            if (key.Contains(':'))
            {
                var splits = key.Split(':');
                if (splits.Length != 2)
                    throw new Exception($"Invalid namespace format: {key}");
                
                writer.WriteAttributeString(splits[1], splits[0], val);
                continue;
            }
            
            writer.WriteAttributeString(key, val);
        }

        var thisChildren = _childIndices.AsSpan(node.FirstChildIndex, node.ChildCount);
        foreach (var childIndex in thisChildren)
        {
            WriteXmlElement(writer, childIndex);
        }

        writer.WriteEndElement();
    }

    public XDocument ToXml()
    {
        var doc = new XDocument();
        using var writer = doc.CreateWriter();
        WriteTo(writer);
        return doc;
    }

    private static string GetString(Span<byte> data, int offset)
    {
        var relevantData = data[offset..];
        var length = relevantData.IndexOf((byte)'\0');

        if (length == 0)
            return "empty";

        return Encoding.ASCII.GetString(relevantData[..length]);
    }

    public override string ToString()
    {
        using var sw = new StringWriter();

        using (var writer = XmlWriter.Create(sw, new XmlWriterSettings { Indent = true }))
        {
            WriteTo(writer);
        }

        return sw.ToString();
    }

    public void Save(string entry)
    {
        using var writer = XmlWriter.Create(entry, new XmlWriterSettings { Indent = true });
        WriteTo(writer);
    }

    public HashSet<string> EnumerateAllStrings()
    {
        var strings = new HashSet<string>();
        foreach (var node in _nodes)
        {
            strings.Add(GetString(_stringData, (int)node.TagStringOffset));
        }

        foreach (var attribute in _attributes)
        {
            strings.Add(GetString(_stringData, (int)attribute.KeyStringOffset));
            strings.Add(GetString(_stringData, (int)attribute.ValueStringOffset));
        }

        return strings;
    }
}