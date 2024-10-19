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
        var isoMeshSubsets = reader.ReadSpan<IvoMeshSubset>(meshChunk.NumberOfSubmeshes);
        
        //todo
        
        return new IvoSkin2();
    }

    public void WriteXmlTo(TextWriter writer)
    {
        throw new NotImplementedException();
    }
}

public class MeshChunk
{
    public int NumberOfSubmeshes { get; init; }
    
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
            NumberOfSubmeshes = (int)numberOfSubmeshes
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