using System.Runtime.InteropServices;

namespace StarBreaker.Common;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct ColorBgra(byte r, byte g, byte b)
{
    public byte B { get; } = b;
    public byte G { get; } = g;
    public byte R { get; } = r;

    //Alpha seems to be unused. Keep it for alignment.
    private readonly byte _A;
    
    public override string ToString() => $"#{R:X2}{G:X2}{B:X2}";
}