using System.Runtime.InteropServices;

namespace StarBreaker.CryXmlB;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct CryXmlHeader
{
    public readonly uint XmlSize;
    public readonly uint NodeTablePosition;
    public readonly uint NodeCount;
    public readonly uint AttributeTablePosition;
    public readonly uint AttributeCount;
    public readonly uint ChildTablePosition;
    public readonly uint ChildCount;
    public readonly uint StringDataPosition;
    public readonly uint StringDataSize;
}