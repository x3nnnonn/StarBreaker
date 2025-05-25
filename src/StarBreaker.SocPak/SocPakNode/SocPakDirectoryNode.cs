using System.Collections.Concurrent;

namespace StarBreaker.SocPak;

public sealed class SocPakDirectoryNode : ISocPakNode
{
    public ISocPakNode? Parent { get; }
    public string Name { get; }
    public ConcurrentDictionary<string, ISocPakNode> Children { get; } = new(StringComparer.OrdinalIgnoreCase);

    public SocPakDirectoryNode(string name, ISocPakNode? parent = null)
    {
        Name = name;
        Parent = parent;
    }

    public SocPakDirectoryNode GetOrCreateChild(string name)
    {
        if (Children.TryGetValue(name, out var existingChild) && existingChild is SocPakDirectoryNode existingDir)
        {
            return existingDir;
        }

        var newChild = new SocPakDirectoryNode(name, this);
        Children[name] = newChild;
        return newChild;
    }

    public void AddFileChild(SocPakFileNode fileNode)
    {
        Children[fileNode.Name] = fileNode;
    }

    public IEnumerable<SocPakDirectoryNode> GetDirectories()
    {
        return Children.Values.OfType<SocPakDirectoryNode>();
    }

    public IEnumerable<SocPakFileNode> GetFiles()
    {
        return Children.Values.OfType<SocPakFileNode>();
    }

    public IEnumerable<ISocPakNode> GetAllNodes()
    {
        return Children.Values;
    }
} 