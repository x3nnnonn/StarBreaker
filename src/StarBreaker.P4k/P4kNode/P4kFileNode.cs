using System.Diagnostics;

namespace StarBreaker.P4k;

[DebuggerDisplay("{P4KEntry.Name}")]
public sealed class P4kFileNode : IP4kFileNode
{
    private readonly IP4kFile _p4k;

    public P4kEntry P4KEntry { get; }
    public ulong Size => P4KEntry.UncompressedSize;
    public string Name => P4KEntry.Name;
    public IP4kDirectoryNode Directory { get; }
    public Stream Open() => _p4k.OpenStream(P4KEntry);

    public P4kFileNode(IP4kDirectoryNode directory, P4kEntry p4KEntry, IP4kFile p4kFile)
    {
        Directory = directory;
        P4KEntry = p4KEntry;
        _p4k = p4kFile;
    }
}