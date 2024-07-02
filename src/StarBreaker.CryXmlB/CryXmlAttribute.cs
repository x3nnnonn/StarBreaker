using System.Runtime.InteropServices;

namespace StarBreaker.CryXmlB;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct CryXmlAttribute
{
    public readonly uint KeyStringOffset;
    public readonly uint ValueStringOffset;
}