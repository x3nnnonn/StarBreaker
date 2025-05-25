using System.Diagnostics;

namespace StarBreaker.P4k;

[DebuggerDisplay("{P4KEntry.Name}")]
public sealed class P4kFileNode : IP4kNode
{
    public IP4kNode Parent { get; }

    public P4kEntry P4KEntry { get; }

    public P4kFileNode(P4kEntry p4KEntry, IP4kNode parent)
    {
        P4KEntry = p4KEntry;
        Parent = parent;
    }
}