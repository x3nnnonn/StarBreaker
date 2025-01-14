using System.Diagnostics;

namespace StarBreaker.P4k;

[DebuggerDisplay("{ZipEntry.Name}")]
public sealed class P4kFileNode : IP4kNode
{
    public IP4kNode Parent { get; }

    public ZipEntry ZipEntry { get; }

    public P4kFileNode(ZipEntry zipEntry, IP4kNode parent)
    {
        ZipEntry = zipEntry;
        Parent = parent;
    }
}