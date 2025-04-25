using StarBreaker.P4k;

namespace StarBreaker;

public class FakeP4kFile : IP4kFile
{
    public string P4KPath { get; }
    public ZipEntry[] Entries { get; }
    public P4kDirectoryNode Root { get; }

    public FakeP4kFile(string path, ZipEntry[] entries, P4kDirectoryNode root)
    {
        P4KPath = path;
        Root = root;
        Entries = entries;
    }

    public Stream OpenStream(ZipEntry entry) => new MemoryStream(new byte[entry.UncompressedSize]);

    public byte[] OpenInMemory(ZipEntry entry) => new byte[entry.UncompressedSize];
}