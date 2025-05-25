using System.Diagnostics;

namespace StarBreaker.P4k;

[DebuggerDisplay("{FullPath} ({Status}) - {Children.Count} children")]
public sealed class P4kComparisonDirectoryNode : IP4kComparisonNode
{
    public IP4kComparisonNode? Parent { get; }
    public string Name { get; }
    public string FullPath { get; }
    public P4kComparisonStatus Status { get; private set; }
    public Dictionary<string, IP4kComparisonNode> Children { get; }
    
    public P4kComparisonDirectoryNode(
        string name, 
        string fullPath, 
        P4kComparisonStatus status,
        IP4kComparisonNode? parent)
    {
        Name = name;
        FullPath = fullPath;
        Status = status;
        Parent = parent;
        Children = new Dictionary<string, IP4kComparisonNode>();
    }
    
    public void AddChild(IP4kComparisonNode child)
    {
        Children[child.Name] = child;
    }
    
    // Update directory status based on children
    public void UpdateStatus()
    {
        if (!Children.Any())
        {
            Status = P4kComparisonStatus.Unchanged;
            return;
        }
            
        var hasChanges = Children.Values.Any(c => c.Status != P4kComparisonStatus.Unchanged);
        Status = hasChanges ? P4kComparisonStatus.Modified : P4kComparisonStatus.Unchanged;
    }
    
    // Get all file nodes recursively
    public IEnumerable<P4kComparisonFileNode> GetAllFiles()
    {
        foreach (var child in Children.Values)
        {
            if (child is P4kComparisonFileNode fileNode)
                yield return fileNode;
            else if (child is P4kComparisonDirectoryNode dirNode)
                foreach (var file in dirNode.GetAllFiles())
                    yield return file;
        }
    }
} 