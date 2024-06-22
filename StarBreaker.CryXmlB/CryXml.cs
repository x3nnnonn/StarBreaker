using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace StarBreaker.CryXmlB;

public readonly struct CryXml
{
    private static ReadOnlySpan<byte> magic => "CryXmlB\0"u8;
    private const int magicLength = 8;
    private readonly byte[] _data;
    
    public static bool TryOpen(byte[] data, out CryXml cryXml)
    {
        if (IsCryXmlB(data))
        {
            cryXml = new CryXml(data);
            return true;
        }

        cryXml = default;
        return false;
    }
    
    public CryXml(byte[] data)
    {
        _data = data;
    }
    
    public CryXml(Stream stream)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        _data = ms.ToArray();
        
        if (!IsCryXmlB(_data))
            throw new Exception("Invalid CryXmlB file");
    }
    
    public CryXml(BinaryReader reader)
    {
        _data = reader.ReadBytes((int)reader.BaseStream.Length);
        
        if (!IsCryXmlB(_data))
            throw new Exception("Invalid CryXmlB file");
    }
    
    public static bool IsCryXmlB(ReadOnlySpan<byte> data)
    {
        return data.Length > magicLength && data[..magicLength].SequenceEqual(magic);
    }

    public void WriteXml(Stream stream)
    {
        using var writer = new StreamWriter(stream);
        WriteXml(writer);
    }
    
    public void WriteXml(TextWriter writer)
    {
        var cryXmlB = _data.AsSpan();
        var header = MemoryMarshal.Read<CryXmlHeader>(cryXmlB[magicLength..]);
        var nodes = MemoryMarshal.Cast<byte, CryXmlNode>(cryXmlB.Slice((int)header.NodeTablePosition, (int)header.NodeCount * Unsafe.SizeOf<CryXmlNode>()));
        var childIndices = MemoryMarshal.Cast<byte, int>(cryXmlB.Slice((int)header.ChildTablePosition, (int)header.ChildCount * sizeof(int)));
        var attributes = MemoryMarshal.Cast<byte, CryXmlAttribute>(cryXmlB.Slice((int)header.AttributeTablePosition, (int)header.AttributeCount * Unsafe.SizeOf<CryXmlAttribute>()));
        var stringData = cryXmlB.Slice((int)header.StringDataPosition, (int)header.StringDataSize);

        if (nodes[0].ParentIndex != -1)
            throw new Exception("Root node has parent");
        
        WriteXmlElement(writer, 0, 0, nodes, attributes, childIndices, stringData);
    }

    private static void WriteXmlElement(TextWriter writer, int depth, int nodeIndex, Span<CryXmlNode> nodes, Span<CryXmlAttribute> attributes, Span<int> childIndices, Span<byte> stringData)
    {
        ref readonly var node = ref nodes[nodeIndex];

        for (var i = 0; i < depth; i++)
        {
            writer.Write(' ');
            writer.Write(' ');
        }

        writer.Write('<');
        writer.WriteString(stringData, (int)node.TagStringOffset);

        for (var i = 0; i < node.AttributeCount; i++)
        {
            var attributeIndex = node.FirstAttributeIndex + i;
            ref readonly var attribute = ref attributes[attributeIndex];
            writer.Write(' ');
            writer.WriteString(stringData, (int)attribute.KeyStringOffset);
            writer.Write('=');
            writer.Write('\"');
            writer.WriteString(stringData, (int)attribute.ValueStringOffset);
            writer.Write('\"');
        }

        var stringElementLength = stringData[(int)node.ItemType..].IndexOf((byte)'\0');
        var hasStringElement = stringElementLength != 0;
        var hasChildren = node.ChildCount != 0;
        
        if (!hasChildren && !hasStringElement)
        {
            writer.WriteLine('/');
            writer.WriteLine('>');
            return;
        }

        writer.Write('>');

        if (hasStringElement && !hasChildren)
        {
            writer.WriteString(stringData, (int)node.ItemType);
            writer.Write('<');
            writer.Write('/');
            writer.WriteString(stringData, (int)node.TagStringOffset);
            writer.WriteLine('>');
            return;
        }

        if (hasStringElement)
        {
            writer.WriteLine();
            writer.WriteString(stringData, (int)node.ItemType);
            writer.WriteLine();
        }

        if (!hasStringElement && hasChildren)
        {
            writer.WriteLine();
        }

        for (var i = 0; i < node.ChildCount; i++)
        {
            var childIndex = childIndices[node.FirstChildIndex + i];

            WriteXmlElement(writer, depth + 1, childIndex, nodes, attributes, childIndices, stringData);
        }

        for (var i = 0; i < depth; i++)
        {
            writer.Write(' ');
            writer.Write(' ');
        }

        writer.Write('<');
        writer.Write('/');
        writer.WriteString(stringData, (int)node.TagStringOffset);
        writer.WriteLine('>');
    }
}