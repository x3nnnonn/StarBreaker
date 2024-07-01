using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using StarBreaker.Common;

namespace StarBreaker.CryChunkFile;

public class CompiledBoneChunk : IChunk
{
    public string[] BoneNames { get; private set; }
    public CompiledBoneData[] Bones { get; private set; }
    
    public static IChunk Read(ref SpanReader reader)
    {
        var chunk = new CompiledBoneChunk();

        var numBones = reader.ReadUInt32();
        var bones = reader.ReadSpan<CompiledBoneData>((int)numBones);
        var names = new string[numBones];
        for (var i = 0; i < numBones; i++)
        {
            var bytes = reader.RemainingBytes;
            var length = bytes.IndexOf((byte)0);
            names[i] = Encoding.ASCII.GetString(bytes[..length]);
            reader.Advance(length + 1);
        }
        
        chunk.Bones = bones.ToArray();
        chunk.BoneNames = names;
        
        return chunk;
    }

    public void WriteXmlTo(TextWriter writer)
    {
        throw new NotImplementedException();
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly record struct CompiledBoneData
    {
        public readonly int ControllerId;
        public readonly uint LimbId;
        public readonly int OffsetParent;
        public readonly Quaternion RelativeQuat;
        public readonly Vector3 RelativeTranslation;
        public readonly Quaternion WorldQuat;
        public readonly Vector3 WorldTranslation;
    }
}
