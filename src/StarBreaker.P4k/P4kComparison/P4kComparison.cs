namespace StarBreaker.P4k;

public static class P4kComparison
{
    public static P4kComparisonDirectoryNode Compare(IP4kFile leftP4k, IP4kFile rightP4k, IProgress<double>? progress = null)
    {
        progress?.Report(0);
        
        // Create root comparison node
        var root = new P4kComparisonDirectoryNode("Root", "", P4kComparisonStatus.Unchanged, null);
        
        // Build dictionaries for fast lookup by path
        var leftFiles = BuildFileIndex(leftP4k);
        var rightFiles = BuildFileIndex(rightP4k);
        
        progress?.Report(0.1);
        
        // Get all unique paths from both P4Ks
        var allPaths = leftFiles.Keys.Union(rightFiles.Keys).ToList();
        var totalPaths = allPaths.Count;
        var processedPaths = 0;
        
        foreach (var path in allPaths)
        {
            var leftEntry = leftFiles.GetValueOrDefault(path);
            var rightEntry = rightFiles.GetValueOrDefault(path);
            
            var status = DetermineStatus(leftEntry, rightEntry);
            AddFileToComparisonTree(root, path, status, leftEntry, rightEntry);
            
            processedPaths++;
            if (processedPaths % 100 == 0) // Update progress every 100 files
            {
                progress?.Report(0.1 + (processedPaths / (double)totalPaths) * 0.8);
            }
        }
        
        // Update directory statuses based on their children
        UpdateDirectoryStatuses(root);
        
        progress?.Report(1.0);
        return root;
    }
    
    private static Dictionary<string, ZipEntry> BuildFileIndex(IP4kFile p4kFile)
    {
        var fileIndex = new Dictionary<string, ZipEntry>();
        
        foreach (var entry in p4kFile.Entries)
        {
            fileIndex[entry.Name] = entry;
        }
        
        return fileIndex;
    }
    
    private static P4kComparisonStatus DetermineStatus(ZipEntry? leftEntry, ZipEntry? rightEntry)
    {
        if (leftEntry == null && rightEntry != null)
            return P4kComparisonStatus.Added;
            
        if (leftEntry != null && rightEntry == null)
            return P4kComparisonStatus.Removed;
            
        if (leftEntry != null && rightEntry != null)
        {
            // Compare by CRC32 first (fastest), then size
            if (leftEntry.Crc32 != rightEntry.Crc32 || 
                leftEntry.UncompressedSize != rightEntry.UncompressedSize)
                return P4kComparisonStatus.Modified;
        }
        
        return P4kComparisonStatus.Unchanged;
    }
    
    private static void AddFileToComparisonTree(
        P4kComparisonDirectoryNode root, 
        string filePath, 
        P4kComparisonStatus status,
        ZipEntry? leftEntry,
        ZipEntry? rightEntry)
    {
        var pathParts = filePath.Split('\\');
        var current = root;
        var currentPath = "";
        
        // Create directory structure
        for (int i = 0; i < pathParts.Length - 1; i++)
        {
            var dirName = pathParts[i];
            currentPath = string.IsNullOrEmpty(currentPath) ? dirName : currentPath + "\\" + dirName;
            
            if (!current.Children.TryGetValue(dirName, out var child))
            {
                child = new P4kComparisonDirectoryNode(dirName, currentPath, P4kComparisonStatus.Unchanged, current);
                current.AddChild(child);
            }
            
            current = (P4kComparisonDirectoryNode)child;
        }
        
        // Add the file
        var fileName = pathParts[^1];
        var fileNode = new P4kComparisonFileNode(fileName, filePath, status, leftEntry, rightEntry, current);
        current.AddChild(fileNode);
    }
    
    private static void UpdateDirectoryStatuses(P4kComparisonDirectoryNode directory)
    {
        // First, recursively update all child directories
        foreach (var child in directory.Children.Values.OfType<P4kComparisonDirectoryNode>())
        {
            UpdateDirectoryStatuses(child);
        }
        
        // Then update this directory's status based on its children
        directory.UpdateStatus();
    }

    /// <summary>
    /// Analyzes a comparison result and returns statistics about the changes
    /// </summary>
    public static ComparisonStats AnalyzeComparison(P4kComparisonDirectoryNode root)
    {
        var allFiles = root.GetAllFiles().ToList();
        
        return new ComparisonStats
        {
            TotalFiles = allFiles.Count,
            AddedFiles = allFiles.Count(f => f.Status == P4kComparisonStatus.Added),
            RemovedFiles = allFiles.Count(f => f.Status == P4kComparisonStatus.Removed),
            ModifiedFiles = allFiles.Count(f => f.Status == P4kComparisonStatus.Modified),
            UnchangedFiles = allFiles.Count(f => f.Status == P4kComparisonStatus.Unchanged)
        };
    }
}

/// <summary>
/// Statistics about a P4K comparison result
/// </summary>
public record ComparisonStats
{
    public int TotalFiles { get; init; }
    public int AddedFiles { get; init; }
    public int RemovedFiles { get; init; }
    public int ModifiedFiles { get; init; }
    public int UnchangedFiles { get; init; }
} 