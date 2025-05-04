using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using StarBreaker.Common;

namespace StarBreaker.CryChunkFile;

internal class NodeMeshCombosChunk : IChunk
{
    public static IChunk Read(ref SpanReader reader)
    {
        var xx = Unsafe.SizeOf<Padding>();
        var zeropad= reader.ReadUInt32();
        var numberOfNodes = reader.ReadUInt32();
        var numberOfMeshes = reader.ReadUInt32();
        var unknown2 = reader.ReadUInt32();
        var numberOfMeshSubsets = reader.ReadUInt32();
        var stringTableSize = reader.ReadUInt32();
        var unknown1 = reader.ReadUInt32();
        var unknown3 = reader.ReadUInt32();
        
        var combos = reader.ReadSpan<NodeMeshCombo>((int)numberOfNodes);
        var unknownIndices = reader.ReadSpan<ushort>((int)unknown2);
        var materialIndices = reader.ReadSpan<ushort>((int)numberOfMeshSubsets);
        var nodeNames = new string[numberOfNodes];
        for (var i = 0; i < numberOfNodes; i++)
        {
            var name = reader.ReadNullTerminatedString();
            nodeNames[i] = name;
        }
        
        return new UnknownChunk();
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct NodeMeshCombo
    {
        public readonly Matrix3x4 WorldToBone;
        public readonly Matrix3x4 BoneToWorld;
        public readonly Vector3 ScaleComponent;
        public readonly uint Id;
        public readonly uint Unknown2;
        public readonly ushort ParentIndex;
        public readonly IvoGeometryType GeometryType;
        public readonly Vector3 BoundingBoxMin;
        public readonly Vector3 BoundingBoxMax;
        public readonly ulong Unknown3_1;
        public readonly ulong Unknown3_2;
        public readonly uint NumberOfVertices;
        public readonly ushort NumberOfChildren;
        public readonly ushort MeshChunkId;

        //40 bytes of unknown data at the end
        private readonly Padding _padding;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private readonly struct Padding
    {
        private readonly ulong _pad1;
        private readonly ulong _pad2;
        private readonly ulong _pad3;
        private readonly ulong _pad4;
        private readonly ulong _pad5;
    }
}

public enum IvoGeometryType : short
{
    Geometry = 0x0,
    Helper2 = 0x2,
    Helper3 = 0x3       // Have only seen 0 (geometry) and 2/3 (helper) in the wild.
}