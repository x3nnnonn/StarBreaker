using System.Diagnostics;

namespace StarBreaker.P4k;

[DebuggerDisplay("{P4KEntry.Name} (SOCPAK) - {Children.Count} children")]
public sealed class P4kSocPakFileNode : IP4kNode, IDisposable
{
    public IP4kNode Parent { get; }
    public P4kEntry P4KEntry { get; }
    public string Name => Path.GetFileNameWithoutExtension(P4KEntry.Name);
    
    /// <summary>
    /// Gets the loaded SOCPAK file instance, or null if it couldn't be loaded
    /// </summary>
    public P4kFile? SocPakFile => _socPakFile.Value;
    
    private readonly Lazy<Dictionary<string, IP4kNode>> _children;
    public Dictionary<string, IP4kNode> Children => _children.Value;
    
    private readonly IP4kFile _parentP4kFile;
    private readonly Lazy<P4kFile?> _socPakFile;
    private string? _tempFilePath;

    public P4kSocPakFileNode(P4kEntry p4KEntry, IP4kNode parent, IP4kFile parentP4kFile)
    {
        P4KEntry = p4KEntry;
        Parent = parent;
        _parentP4kFile = parentP4kFile;
        
        // Lazy load children to avoid performance issues when not expanded
        _children = new Lazy<Dictionary<string, IP4kNode>>(LoadSocPakContents);
        
        // Lazy load the SOCPAK P4K file instance
        _socPakFile = new Lazy<P4kFile?>(LoadSocPakFile);
    }
    
    private P4kFile? LoadSocPakFile()
    {
        try
        {
            // Extract SOCPAK contents and save to a temporary file
            using var socPakStream = _parentP4kFile.OpenStream(P4KEntry);
            using var memoryStream = new MemoryStream();
            socPakStream.CopyTo(memoryStream);
            var socPakBytes = memoryStream.ToArray();
            
            // Create a temporary file that will persist until this node is disposed
            _tempFilePath = Path.GetTempFileName();
            File.WriteAllBytes(_tempFilePath, socPakBytes);
            
            // Load P4K file from the temporary file
            return P4kFile.FromFile(_tempFilePath);
        }
        catch (Exception)
        {
            // Clean up temp file if loading failed
            if (_tempFilePath != null && File.Exists(_tempFilePath))
            {
                try
                {
                    File.Delete(_tempFilePath);
                }
                catch
                {
                    // Ignore cleanup errors
                }
                _tempFilePath = null;
            }
            
            // If we can't load the SOCPAK, return null
            return null;
        }
    }
    
    private Dictionary<string, IP4kNode> LoadSocPakContents()
    {
        try
        {
            var socPakFile = _socPakFile.Value;
            if (socPakFile == null)
            {
                return new Dictionary<string, IP4kNode>();
            }
            
            // Build the proper directory structure from all entries in the SOCPAK
            var rootNode = new P4kSocPakDirectoryNode("", this);
            
            // Insert all entries to build the directory structure
            foreach (var entry in socPakFile.Entries)
            {
                InsertSocPakEntry(rootNode, entry, this);
            }
            
            // Return the children of our virtual root
            return rootNode.Children;
        }
        catch (Exception)
        {
            // If we can't load the SOCPAK, return empty children
            return new Dictionary<string, IP4kNode>();
        }
    }
    
    private static void InsertSocPakEntry(P4kSocPakDirectoryNode parentNode, P4kEntry entry, P4kSocPakFileNode socPakFileNode)
    {
        var pathParts = entry.Name.Split('/', StringSplitOptions.RemoveEmptyEntries);
        
        var currentNode = parentNode;
        
        // Navigate/create directory structure
        for (int i = 0; i < pathParts.Length - 1; i++)
        {
            var dirName = pathParts[i];
            
            if (!currentNode.Children.TryGetValue(dirName, out var childNode))
            {
                // Create new directory node
                var newDirNode = new P4kSocPakDirectoryNode(dirName, currentNode);
                currentNode.Children[dirName] = newDirNode;
                currentNode = newDirNode;
            }
            else if (childNode is P4kSocPakDirectoryNode dirNode)
            {
                currentNode = dirNode;
            }
            else
            {
                // Conflict: there's already a file with this name
                // This shouldn't happen in well-formed archives, but handle gracefully
                return;
            }
        }
        
        // Add the file to the final directory
        var fileName = pathParts[^1];
        if (!currentNode.Children.ContainsKey(fileName))
        {
            currentNode.Children[fileName] = new P4kSocPakChildFileNode(entry, currentNode);
        }
    }
    
    private static IP4kNode ConvertToSocPakChild(IP4kNode sourceNode, IP4kNode newParent)
    {
        return sourceNode switch
        {
            P4kDirectoryNode sourceDir => new P4kSocPakDirectoryNode(sourceDir, newParent),
            P4kFileNode sourceFile => new P4kSocPakChildFileNode(sourceFile.P4KEntry, newParent),
            _ => throw new ArgumentException($"Unsupported node type: {sourceNode.GetType()}")
        };
    }
    
    public static bool IsSocPakFile(string fileName)
    {
        return fileName.EndsWith(".socpak", StringComparison.OrdinalIgnoreCase);
    }
    
    public void Dispose()
    {
        // Clean up the temporary file when the node is disposed
        if (_tempFilePath != null && File.Exists(_tempFilePath))
        {
            try
            {
                File.Delete(_tempFilePath);
            }
            catch
            {
                // Ignore cleanup errors - temp files will be cleaned up by OS eventually
            }
            _tempFilePath = null;
        }
    }
}

/// <summary>
/// Represents a directory inside a SOCPAK file
/// </summary>
[DebuggerDisplay("{Name} (SOCPAK Dir) - {Children.Count} children")]
public sealed class P4kSocPakDirectoryNode : IP4kNode
{
    public IP4kNode Parent { get; }
    public string Name { get; }
    public Dictionary<string, IP4kNode> Children { get; }

    public P4kSocPakDirectoryNode(P4kDirectoryNode sourceDir, IP4kNode parent)
    {
        Name = sourceDir.Name;
        Parent = parent;
        Children = new Dictionary<string, IP4kNode>();
        
        // Convert all children
        foreach (var (key, child) in sourceDir.Children)
        {
            Children[key] = ConvertChild(child, this);
        }
    }
    
    public P4kSocPakDirectoryNode(string name, IP4kNode parent)
    {
        Name = name;
        Parent = parent;
        Children = new Dictionary<string, IP4kNode>();
    }
    
    private static IP4kNode ConvertChild(IP4kNode sourceNode, IP4kNode newParent)
    {
        return sourceNode switch
        {
            P4kDirectoryNode sourceDir => new P4kSocPakDirectoryNode(sourceDir, newParent),
            P4kFileNode sourceFile => new P4kSocPakChildFileNode(sourceFile.P4KEntry, newParent),
            _ => throw new ArgumentException($"Unsupported node type: {sourceNode.GetType()}")
        };
    }
}

/// <summary>
/// Represents a file inside a SOCPAK file
/// </summary>
[DebuggerDisplay("{Name} (SOCPAK File)")]
public sealed class P4kSocPakChildFileNode : IP4kNode
{
    public IP4kNode Parent { get; }
    public P4kEntry P4KEntry { get; }
    public string Name => Path.GetFileName(P4KEntry.Name);

    public P4kSocPakChildFileNode(P4kEntry p4KEntry, IP4kNode parent)
    {
        P4KEntry = p4KEntry;
        Parent = parent;
    }
} 