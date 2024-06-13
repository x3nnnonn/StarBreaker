using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

namespace StarBreaker.Forge;

[DebuggerDisplay("{ToString()}")]
public readonly record struct CigGuid
{
    internal readonly short _c;
    internal readonly short _b;
    internal readonly int _a;
    internal readonly byte _k;
    internal readonly byte _j;
    internal readonly byte _i;
    internal readonly byte _h;
    internal readonly byte _g;
    internal readonly byte _f;
    internal readonly byte _e;
    internal readonly byte _d;
    
    private static readonly char[] x2 = ['x', '2'];
    private static readonly char[] x4 = ['x', '4'];
    private static readonly char[] x8 = ['x', '8'];
    
    public override string ToString()
    {
        var sb = new StringBuilder(36);
        sb.Append(_a.ToString("x8"));
        sb.Append('-');
        sb.Append(_b.ToString("x4"));
        sb.Append('-');
        sb.Append(_c.ToString("x4"));
        sb.Append('-');
        sb.Append(_d.ToString("x2"));
        sb.Append(_e.ToString("x2"));
        sb.Append('-');
        sb.Append(_f.ToString("x2"));
        sb.Append(_g.ToString("x2"));
        sb.Append(_h.ToString("x2"));
        sb.Append(_i.ToString("x2"));
        sb.Append(_j.ToString("x2"));
        sb.Append(_k.ToString("x2"));
        return sb.ToString();
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteInto(TextWriter writer)
    {
        Span<char> buffer = stackalloc char[36];
        
        _a.TryFormat(buffer.Slice(0, 8), out _, x8);
        buffer[8] = '-';
        _b.TryFormat(buffer.Slice(9, 4), out _, x4);
        buffer[13] = '-';
        _c.TryFormat(buffer.Slice(14, 4), out _, x4);
        buffer[18] = '-';
        _d.TryFormat(buffer.Slice(19, 2), out _, x2);
        _e.TryFormat(buffer.Slice(21, 2), out _, x2);
        buffer[23] = '-';
        _f.TryFormat(buffer.Slice(24, 2), out _, x2);
        _g.TryFormat(buffer.Slice(26, 2), out _, x2);
        _h.TryFormat(buffer.Slice(28, 2), out _, x2);
        _i.TryFormat(buffer.Slice(30, 2), out _, x2);
        _j.TryFormat(buffer.Slice(32, 2), out _, x2);
        _k.TryFormat(buffer.Slice(34, 2), out _, x2);
        writer.Write(buffer);
    }
}