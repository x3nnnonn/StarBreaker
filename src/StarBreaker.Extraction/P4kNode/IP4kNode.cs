namespace StarBreaker.Extraction;

public interface IP4kNode
{
    string Name { get; }
    ulong Size { get; }
}

public interface IP4kFileNode : IP4kNode
{
    Stream Open();
    string RelativeOutputPath { get; }
}

public interface IP4kDirectoryNode : IP4kNode
{
    Dictionary<string, IP4kNode> Children { get; }

    IEnumerable<IP4kFileNode> CollectAllFiles();
}