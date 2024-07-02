using System.Runtime.InteropServices;

namespace StarBreaker.CryXmlB;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct CryXmlNode
{
    public readonly uint TagStringOffset;
    public readonly uint ItemType;
    public readonly ushort AttributeCount;
    public readonly ushort ChildCount;
    public readonly int ParentIndex;
    public readonly int FirstAttributeIndex;
    public readonly int FirstChildIndex;
    public readonly int Reserved;
}