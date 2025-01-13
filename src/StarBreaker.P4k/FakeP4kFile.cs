namespace StarBreaker.P4k;

public class FakeP4kFile : IP4kFile
{
    public string P4KPath { get; }
    public ZipEntry[] Entries { get; }
    public ZipNode Root { get; }

    public FakeP4kFile(string path, ZipEntry[] entries, ZipNode root)
    {
        P4KPath = path;
        Root = root;
        Entries = entries;
    }

    public Stream OpenStream(ZipEntry entry) => new MemoryStream(new byte[entry.UncompressedSize]);

    public byte[] OpenInMemory(ZipEntry entry) => new byte[entry.UncompressedSize];
}