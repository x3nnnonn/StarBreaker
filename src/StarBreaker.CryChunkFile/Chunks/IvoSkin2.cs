using System.Numerics;
using System.Runtime.InteropServices;
using StarBreaker.Common;

namespace StarBreaker.CryChunkFile;

public class IvoSkin2 : IChunk
{
    public static IChunk Read(ref SpanReader reader)
    {
        var flags = reader.ReadUInt32();
        var meshChunk = MeshChunk.Read(ref reader);
        reader.Advance(116);
        var isoMeshSubsets = reader.ReadSpan<IvoMeshSubset>((int)meshChunk.NumberOfSubmeshes);

        var list = new List<object?>();
        while (reader.Remaining > 0)
        {
            var type = (DatastreamType)reader.ReadUInt32();
            if (type == 0)
                continue;//try again smile
            
            list.Add(type switch
            {
                DatastreamType.IvoNormals2 => null,
                DatastreamType.IvoBoneMap => null,
                DatastreamType.IvoBoneMap32 => null,
                DatastreamType.IvoVertsUvs => IvoVertsUvs.Read(ref reader, meshChunk.NumberOfVertices),
                DatastreamType.IvoNormals => null,
                DatastreamType.IvoTangents => null,
                DatastreamType.IvoColors2 => null,
                DatastreamType.IvoIndices => IvoIndices.Read(ref reader, meshChunk.NumberOfIndices),
                DatastreamType.IvoVertsUvs2 => IvoVertsUvs.Read(ref reader, meshChunk.NumberOfVertices),
                DatastreamType.IvoQTangents => IvoQTangents.Read(ref reader, meshChunk.NumberOfVertices),
                DatastreamType.IvoUnknown => IvoUnknown.Read(ref reader, meshChunk.NumberOfVertices),
                _ => throw new ArgumentOutOfRangeException()
            });
        }

        //todo

        return new IvoSkin2();
    }
}

public class IvoUnknown
{
    public static IvoUnknown? Read(ref SpanReader reader, uint numberOfVertices)
    {
        var bytesPerElement = reader.ReadUInt32();
        reader.Advance((int)(bytesPerElement * numberOfVertices));
        return new IvoUnknown();
    }
}

public class IvoQTangents
{
    public static IvoQTangents Read(ref SpanReader reader, uint numberOfVertices)
    {
        var bytesPerElement = reader.ReadUInt32();

        var qtangents = reader.ReadSpan<Tangent>((int)numberOfVertices);
        

        return new IvoQTangents();
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Tangent
    {
        public Half X;
        public Half Y;
        public Half Z;
        public Half W;
    }
}

public class IvoIndices
{
    public static IvoIndices Read(ref SpanReader reader, uint numberOfIndices)
    {
        var bytesPerElement = reader.ReadUInt32();
        var indices = reader.ReadSpan<ushort>((int)numberOfIndices);
        if (numberOfIndices % 2 == 1)
        {
            reader.Advance(2);
        }
        else
        {
            var peek = (char)reader.Peek<byte>();
            if (peek == 0)
                reader.Advance(4);
        }

        return new IvoIndices();
    }
}

public class IvoVertsUvs
{
    public static IvoVertsUvs Read(ref SpanReader reader, uint numberOfVertices)
    {
        var bytesPerElement = reader.ReadUInt32();
        var uvs = reader.ReadSpan<VertUv>((int)numberOfVertices);

        return new IvoVertsUvs();
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct VertUv
{
    public Vector3<ushort> Vertex;
    private ushort padding;
    public ColorRgba color;
    public UvHalf Uv;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Vector3<T>
{
    public T X;
    public T Y;
    public T Z;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct UvHalf
{
    public Half U;
    public Half V;
}

public class MeshChunk
{
    public uint NumberOfVertices { get; init; }
    public uint NumberOfIndices { get; init; }
    public uint NumberOfSubmeshes { get; init; }


    public static MeshChunk Read(ref SpanReader reader)
    {
        var flags2 = reader.ReadUInt32();
        var numberOfVertices = reader.ReadUInt32();
        var numberOfIndices = reader.ReadUInt32();
        var numberOfSubmeshes = reader.ReadUInt32();
        var unknown = reader.ReadUInt32();
        var minBounds = reader.Read<Vector3>();
        var maxBounds = reader.Read<Vector3>();
        var unknown2 = reader.ReadUInt32();

        return new MeshChunk
        {
            NumberOfVertices = numberOfVertices,
            NumberOfIndices = numberOfIndices,
            NumberOfSubmeshes = numberOfSubmeshes
        };
    }

    public void WriteXmlTo(TextWriter writer)
    {
        throw new NotImplementedException();
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct IvoMeshSubset
{
    public readonly uint MatId;
    public readonly uint FirstIndex;
    public readonly uint NumIndices;
    public readonly uint FirstVertex;
    public readonly uint NumVertices;
    public readonly float Radius;
    public readonly Vector3 Center;
    public readonly uint Unknown0;
    public readonly uint Unknown1;
    public readonly uint Unknown2;
}

public enum DatastreamType : uint
{
    IvoNormals2 = 0x38A581FE,
    IvoBoneMap = 0x677C7B23,
    IvoBoneMap32 = 0x6ECA3708,
    IvoVertsUvs = 0x91329AE9,
    IvoNormals = 0x9CF3F615,
    IvoTangents = 0xB95E9A1B,
    IvoColors2 = 0xD9EED421,
    IvoIndices = 0xEECDC168,
    IvoVertsUvs2 = 0xB3A70D5E,
    IvoQTangents = 0xEE057252,
    IvoUnknown = 0x9D51C5EE,
};