namespace StarBreaker.P4k;

public interface IP4kFile
{
    string P4KPath { get; }
    P4kEntry[] Entries { get; }
    P4kDirectoryNode Root { get; }
    Stream OpenStream(P4kEntry entry);
}