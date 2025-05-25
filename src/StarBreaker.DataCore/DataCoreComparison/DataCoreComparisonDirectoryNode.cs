using System.Diagnostics;

namespace StarBreaker.DataCore;

[DebuggerDisplay("{FullPath} ({Status}) - {Children.Count} children")]
public sealed class DataCoreComparisonDirectoryNode : IDataCoreComparisonNode
{
    public IDataCoreComparisonNode? Parent { get; }
    public string Name { get; }
    public string FullPath { get; }
    public DataCoreComparisonStatus Status { get; private set; }
    
    public Dictionary<string, IDataCoreComparisonNode> Children { get; } = new();
    
    public DataCoreComparisonDirectoryNode(
        string name, 
        string fullPath, 
        DataCoreComparisonStatus status,
        IDataCoreComparisonNode? parent)
    {
        Name = name;
        FullPath = fullPath;
        Status = status;
        Parent = parent;
    }
    
    public void AddChild(IDataCoreComparisonNode child)
    {
        Children[child.Name] = child;
    }
    
    /// <summary>
    /// Updates this directory's status based on its children's statuses
    /// </summary>
    public void UpdateStatus()
    {
        if (Children.Count == 0)
        {
            Status = DataCoreComparisonStatus.Unchanged;
            return;
        }
        
        var hasAdded = false;
        var hasRemoved = false;
        var hasModified = false;
        
        foreach (var child in Children.Values)
        {
            switch (child.Status)
            {
                case DataCoreComparisonStatus.Added:
                    hasAdded = true;
                    break;
                case DataCoreComparisonStatus.Removed:
                    hasRemoved = true;
                    break;
                case DataCoreComparisonStatus.Modified:
                    hasModified = true;
                    break;
            }
        }
        
        // Directory is modified if it has any changes
        if (hasAdded || hasRemoved || hasModified)
        {
            Status = DataCoreComparisonStatus.Modified;
        }
        else
        {
            Status = DataCoreComparisonStatus.Unchanged;
        }
    }
    
    /// <summary>
    /// Gets all file nodes recursively
    /// </summary>
    public IEnumerable<DataCoreComparisonFileNode> GetAllFiles()
    {
        foreach (var child in Children.Values)
        {
            if (child is DataCoreComparisonFileNode fileNode)
            {
                yield return fileNode;
            }
            else if (child is DataCoreComparisonDirectoryNode directoryNode)
            {
                foreach (var childFile in directoryNode.GetAllFiles())
                {
                    yield return childFile;
                }
            }
        }
    }
    
    /// <summary>
    /// Gets children filtered by show-only-changed flag
    /// </summary>
    public IDataCoreComparisonNode[] GetFilteredComparisonChildren(bool showOnlyChanged)
    {
        if (!showOnlyChanged)
        {
            return Children.Values.ToArray();
        }
        
        return Children.Values
            .Where(child => child.Status != DataCoreComparisonStatus.Unchanged)
            .ToArray();
    }
} 