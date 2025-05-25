using StarBreaker.P4k;

namespace StarBreaker;

public class FakeP4kFile : IP4kFile
{
    public string P4KPath { get; }
    public P4kEntry[] Entries { get; }
    public P4kDirectoryNode Root { get; }

    public FakeP4kFile(string path, P4kEntry[] entries, P4kDirectoryNode root)
    {
        P4KPath = path;
        Root = root;
        Entries = entries;
    }

    public Stream OpenStream(P4kEntry entry) => new MemoryStream(new byte[entry.UncompressedSize]);
}