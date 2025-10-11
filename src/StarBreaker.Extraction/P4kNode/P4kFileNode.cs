using System.Diagnostics;
using StarBreaker.P4k;

namespace StarBreaker.Extraction;

[DebuggerDisplay("{P4KEntry.Name}")]
public sealed class P4kFileNode : IP4kFileNode
{
    private readonly IP4kFile _p4k;

    public P4kEntry P4KEntry { get; }
    public ulong Size => P4KEntry.UncompressedSize;
    public string Name { get; }
    public Stream Open() => _p4k.OpenStream(P4KEntry);

    public P4kFileNode(P4kEntry p4KEntry, IP4kFile p4kFile, string name)
    {
        _p4k = p4kFile;
        P4KEntry = p4KEntry;
        Name = name;
    }
}