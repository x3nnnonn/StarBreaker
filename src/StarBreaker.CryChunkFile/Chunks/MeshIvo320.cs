using System.Numerics;
using System.Runtime.CompilerServices;
using StarBreaker.Common;

namespace StarBreaker.CryChunkFile;

public class MeshIvo320 : IChunk
{
    public uint Flags1 { get; init; }
    public uint Flags2 { get; init; }
    public uint NumberOfVertices { get; init; }
    public uint NumberOfIndices { get; init; }
    public uint NumberOfSubmeshes { get; init; }
    public uint VerticesData { get; init; }
    public uint NumberOfBuffers { get; init; }
    public uint NormalsData { get; init; }
    public uint UVsData { get; init; }
    public uint ColorsData { get; init; }
    public uint Colors2Data { get; init; }
    public uint IndicesData { get; init; }
    public uint TangentsData { get; init; }
    public uint SHCoeffsData { get; init; }
    public uint ShapeDeformationData { get; init; }
    public uint BoneMapData { get; init; }
    public uint FaceMapData { get; init; }
    public uint VertMatsData { get; init; }
    public uint VertsUVData { get; init; }
    public uint PhysicsData { get; init; }
    public Vector3 MinBound { get; init; }
    public Vector3 MaxBound { get; init; }
    
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

        return new MeshIvo320()
        {
            Flags1 = flags1,
            Flags2 = flags2,
            NumberOfVertices = numVertices,
            NumberOfIndices = numIndices,
            NumberOfSubmeshes = meshSubsets,
            VerticesData = verticesData,
            NumberOfBuffers = numBuffs,
            NormalsData = normalsData,
            UVsData = uvsData,
            ColorsData = colorsData,
            Colors2Data = colors2Data,
            IndicesData = indicesData,
            TangentsData = tangentsData,
            SHCoeffsData = shCoeffsData,
            ShapeDeformationData = shapeDeformationData,
            BoneMapData = boneMapData,
            FaceMapData = faceMapData,
            VertMatsData = vertMatsData,
            VertsUVData = vertsUVData,
            PhysicsData = physicsData[0],
            MinBound = minBound,
            MaxBound = maxBound,
        };
    }
}