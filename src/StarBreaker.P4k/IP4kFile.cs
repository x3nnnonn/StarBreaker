namespace StarBreaker.P4k;

public interface IP4kFile
{
    string P4KPath { get; }
    ZipEntry[] Entries { get; }
    ZipNode Root { get; }
    Stream OpenStream(ZipEntry entry);
    byte[] OpenInMemory(ZipEntry entry);
}