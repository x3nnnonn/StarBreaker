using System.Numerics;
using System.Runtime.CompilerServices;
using StarBreaker.Common;

namespace StarBreaker.CryChunkFile;

public class MeshIvo320 : IChunk
{
    public static IChunk Read(ref SpanReader reader)
    {
        var flags1 = reader.ReadUInt32();
        var flags2 = reader.ReadUInt32();
        
        var numVertices = reader.ReadUInt32();
        var numIndices = reader.ReadUInt32();

        var meshSubsets = reader.ReadUInt32();
        var verticesData = reader.ReadUInt32();
        var numBuffs = reader.ReadUInt32();
        var normalsData = reader.ReadUInt32();
        var uvsData = reader.ReadUInt32();
        var colorsData = reader.ReadUInt32();
        var colors2Data = reader.ReadUInt32();
        var indicesData = reader.ReadUInt32();
        var tangentsData = reader.ReadUInt32();
        var shCoeffsData = reader.ReadUInt32();
        var shapeDeformationData = reader.ReadUInt32();
        var boneMapData = reader.ReadUInt32();
        var faceMapData = reader.ReadUInt32();
        var vertMatsData = reader.ReadUInt32();
        var vertsUVData = reader.ReadUInt32();
        var physicsData = reader.ReadSpan<uint>(4);
        var minBound = reader.Read<Vector3>();
        var maxBound = reader.Read<Vector3>();
        var xxx = Unsafe.SizeOf<Vector3>();

        return new MeshIvo320();
    }

    public void WriteXmlTo(TextWriter writer)
    {
        throw new NotImplementedException();
    }
}