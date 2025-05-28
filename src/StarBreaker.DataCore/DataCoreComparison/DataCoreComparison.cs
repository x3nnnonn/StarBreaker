namespace StarBreaker.DataCore;

public static class DataCoreComparison
{
    public static DataCoreComparisonDirectoryNode Compare(DataCoreDatabase leftDataCore, DataCoreDatabase rightDataCore, IProgress<double>? progress = null)
    {
        progress?.Report(0);
        
        // Create root comparison node
        var root = new DataCoreComparisonDirectoryNode("Root", "", DataCoreComparisonStatus.Unchanged, null);
        
        // Build dictionaries for fast lookup by file name
        var leftRecords = BuildRecordIndex(leftDataCore);
        var rightRecords = BuildRecordIndex(rightDataCore);
        
        // Debug logging to file
        // var debugFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "datacore_comparison_debug.txt");
        // File.WriteAllText(debugFilePath, $"DataCore Comparison Debug Log - {DateTime.Now}\n\n");
        // File.AppendAllText(debugFilePath, $"Left database: {leftRecords.Count} records\n");
        // File.AppendAllText(debugFilePath, $"Right database: {rightRecords.Count} records\n\n");
        
        progress?.Report(0.1);
        
        // Group records by actual file path for comparison
        var leftFileGroups = leftRecords.GroupBy(kvp => kvp.Value.GetFileName(leftDataCore));
        var rightFileGroups = rightRecords.GroupBy(kvp => kvp.Value.GetFileName(rightDataCore));
        
        var leftFileMap = leftFileGroups.ToDictionary(g => g.Key, g => g.ToList());
        var rightFileMap = rightFileGroups.ToDictionary(g => g.Key, g => g.ToList());
        
        // Create DataForge instances for XML extraction
        var leftDataForge = new DataForge<string>(new DataCoreBinaryXml(leftDataCore));
        var rightDataForge = new DataForge<string>(new DataCoreBinaryXml(rightDataCore));
        
        // Get all unique file paths from both DataCores
        var allFilePaths = leftFileMap.Keys.Union(rightFileMap.Keys).ToList();
        var totalPaths = allFilePaths.Count;
        var processedPaths = 0;
        
        // Debug counts to file
        // File.AppendAllText(debugFilePath, $"Total unique file paths to compare: {totalPaths}\n");
        // File.AppendAllText(debugFilePath, $"Left files: {leftFileMap.Count}, Right files: {rightFileMap.Count}\n\n");
        
        // Sample some file paths to see what we're comparing
        // File.AppendAllText(debugFilePath, "Sample left file paths:\n");
        // foreach (var path in leftFileMap.Keys.Take(5))
        // {
        //     File.AppendAllText(debugFilePath, $"  - {path}\n");
        // }
        // File.AppendAllText(debugFilePath, "Sample right file paths:\n");
        // foreach (var path in rightFileMap.Keys.Take(5))
        // {
        //     File.AppendAllText(debugFilePath, $"  - {path}\n");
        // }
        // File.AppendAllText(debugFilePath, "\n");
        
        foreach (var filePath in allFilePaths)
        {
            var leftRecordList = leftFileMap.GetValueOrDefault(filePath, new List<KeyValuePair<string, DataCoreRecord>>());
            var rightRecordList = rightFileMap.GetValueOrDefault(filePath, new List<KeyValuePair<string, DataCoreRecord>>());
            
            // Compare records for this file path using actual XML content
            var status = DetermineFileStatus(leftRecordList, rightRecordList, leftDataCore, rightDataCore, leftDataForge, rightDataForge);
            
            // For the tree node, use the primary record from each side (if any)
            var leftRecord = leftRecordList.FirstOrDefault().Value;
            var rightRecord = rightRecordList.FirstOrDefault().Value;
            
            AddRecordToComparisonTree(root, filePath, status, 
                leftRecordList.Any() ? leftRecord : null, 
                rightRecordList.Any() ? rightRecord : null, 
                leftDataCore, rightDataCore);
            
            processedPaths++;
            if (processedPaths % 100 == 0) // Update progress every 100 files
            {
                progress?.Report(0.1 + (processedPaths / (double)totalPaths) * 0.8);
            }
        }
        
        // Update directory statuses based on their children
        UpdateDirectoryStatuses(root);
        
        // Write final summary to debug file
        // var stats = AnalyzeComparison(root);
        // File.AppendAllText(debugFilePath, $"\n=== FINAL SUMMARY ===\n");
        // File.AppendAllText(debugFilePath, $"Total files: {stats.TotalFiles}\n");
        // File.AppendAllText(debugFilePath, $"Added: {stats.AddedFiles}\n");
        // File.AppendAllText(debugFilePath, $"Removed: {stats.RemovedFiles}\n");
        // File.AppendAllText(debugFilePath, $"Modified: {stats.ModifiedFiles}\n");
        // File.AppendAllText(debugFilePath, $"Unchanged: {stats.UnchangedFiles}\n");
        
        progress?.Report(1.0);
        return root;
    }
    
    private static Dictionary<string, DataCoreRecord> BuildRecordIndex(DataCoreDatabase database)
    {
        var recordIndex = new Dictionary<string, DataCoreRecord>();
        
        foreach (var record in database.RecordDefinitions)
        {
            var fileName = record.GetFileName(database);
            
            // Handle duplicate filenames by using a unique key (filename + record ID)
            var uniqueKey = $"{fileName}#{record.Id}";
            recordIndex[uniqueKey] = record;
        }
        
        return recordIndex;
    }
    
    private static DataCoreComparisonStatus DetermineFileStatus(
        List<KeyValuePair<string, DataCoreRecord>> leftRecords,
        List<KeyValuePair<string, DataCoreRecord>> rightRecords,
        DataCoreDatabase leftDatabase,
        DataCoreDatabase rightDatabase,
        DataForge<string> leftDataForge,
        DataForge<string> rightDataForge)
    {
        // var debugFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "datacore_comparison_debug.txt");
        
        if (!leftRecords.Any() && rightRecords.Any())
        {
            // var filename = rightRecords.First().Value.GetFileName(rightDatabase);
            // File.AppendAllText(debugFilePath, $"File ADDED: {filename}\n");
            return DataCoreComparisonStatus.Added;
        }
            
        if (leftRecords.Any() && !rightRecords.Any())
        {
            // var filename = leftRecords.First().Value.GetFileName(leftDatabase);
            // File.AppendAllText(debugFilePath, $"File REMOVED: {filename}\n");
            return DataCoreComparisonStatus.Removed;
        }
            
        if (leftRecords.Any() && rightRecords.Any())
        {
            // Compare exact record count first - quick check
            if (leftRecords.Count != rightRecords.Count)
            {
                return DataCoreComparisonStatus.Modified;
            }
            
            // Extract and compare actual XML content for each record
            var leftXmlContents = new List<string>();
            var rightXmlContents = new List<string>();
            
            // Extract XML content from left records
            foreach (var leftRecord in leftRecords)
            {
                try
                {
                    if (leftDatabase.MainRecords.Contains(leftRecord.Value.Id))
                    {
                        var xmlContent = leftDataForge.GetFromRecord(leftRecord.Value);
                        leftXmlContents.Add(xmlContent ?? "");
                    }
                    else
                    {
                        // For non-main records, use basic info
                        leftXmlContents.Add(GenerateBasicRecordInfo(leftRecord.Value, leftDatabase));
                    }
                }
                catch
                {
                    // Fallback to basic info on error
                    leftXmlContents.Add(GenerateBasicRecordInfo(leftRecord.Value, leftDatabase));
                }
            }
            
            // Extract XML content from right records
            foreach (var rightRecord in rightRecords)
            {
                try
                {
                    if (rightDatabase.MainRecords.Contains(rightRecord.Value.Id))
                    {
                        var xmlContent = rightDataForge.GetFromRecord(rightRecord.Value);
                        rightXmlContents.Add(xmlContent ?? "");
                    }
                    else
                    {
                        // For non-main records, use basic info
                        rightXmlContents.Add(GenerateBasicRecordInfo(rightRecord.Value, rightDatabase));
                    }
                }
                catch
                {
                    // Fallback to basic info on error
                    rightXmlContents.Add(GenerateBasicRecordInfo(rightRecord.Value, rightDatabase));
                }
            }
            
            // Sort both lists to ensure consistent comparison
            leftXmlContents.Sort();
            rightXmlContents.Sort();
            
            // Compare the XML content
            if (!leftXmlContents.SequenceEqual(rightXmlContents))
            {
                return DataCoreComparisonStatus.Modified;
            }
        }
        
        // If record count and XML content match, consider unchanged
        return DataCoreComparisonStatus.Unchanged;
    }
    
    private static string GenerateBasicRecordInfo(DataCoreRecord record, DataCoreDatabase database)
    {
        var fileName = record.GetFileName(database);
        var recordName = record.GetName(database);
        var structTypeName = database.StructDefinitions[record.StructIndex].GetName(database);
        
        return $"<DataCoreRecord>" +
               $"<RecordId>{record.Id}</RecordId>" +
               $"<RecordName>{recordName}</RecordName>" +
               $"<FileName>{fileName}</FileName>" +
               $"<StructType>{structTypeName}</StructType>" +
               $"<StructSize>{record.StructSize}</StructSize>" +
               $"</DataCoreRecord>";
    }
    
    private static void AddRecordToComparisonTree(
        DataCoreComparisonDirectoryNode root, 
        string filePath, 
        DataCoreComparisonStatus status,
        DataCoreRecord? leftRecord,
        DataCoreRecord? rightRecord,
        DataCoreDatabase? leftDatabase,
        DataCoreDatabase? rightDatabase)
    {
        var pathParts = filePath.Split('\\', '/');
        var current = root;
        var currentPath = "";
        
        // Create directory structure
        for (int i = 0; i < pathParts.Length - 1; i++)
        {
            var dirName = pathParts[i];
            currentPath = string.IsNullOrEmpty(currentPath) ? dirName : currentPath + "\\" + dirName;
            
            if (!current.Children.TryGetValue(dirName, out var child))
            {
                child = new DataCoreComparisonDirectoryNode(dirName, currentPath, DataCoreComparisonStatus.Unchanged, current);
                current.AddChild(child);
            }
            
            current = (DataCoreComparisonDirectoryNode)child;
        }
        
        // Add the file
        var fileName = pathParts[^1];
        var fileNode = new DataCoreComparisonFileNode(fileName, filePath, status, leftRecord, rightRecord, leftDatabase, rightDatabase, current);
        current.AddChild(fileNode);
    }
    
    private static void UpdateDirectoryStatuses(DataCoreComparisonDirectoryNode directory)
    {
        // First, recursively update all child directories
        foreach (var child in directory.Children.Values.OfType<DataCoreComparisonDirectoryNode>())
        {
            UpdateDirectoryStatuses(child);
        }
        
        // Then update this directory's status based on its children
        directory.UpdateStatus();
    }

    /// <summary>
    /// Analyzes a comparison result and returns statistics about the changes
    /// </summary>
    public static DataCoreComparisonStats AnalyzeComparison(DataCoreComparisonDirectoryNode root)
    {
        var allFiles = root.GetAllFiles().ToList();
        
        return new DataCoreComparisonStats
        {
            TotalFiles = allFiles.Count,
            AddedFiles = allFiles.Count(f => f.Status == DataCoreComparisonStatus.Added),
            RemovedFiles = allFiles.Count(f => f.Status == DataCoreComparisonStatus.Removed),
            ModifiedFiles = allFiles.Count(f => f.Status == DataCoreComparisonStatus.Modified),
            UnchangedFiles = allFiles.Count(f => f.Status == DataCoreComparisonStatus.Unchanged)
        };
    }
}

/// <summary>
/// Statistics about a DataCore comparison result
/// </summary>
public record DataCoreComparisonStats
{
    public int TotalFiles { get; init; }
    public int AddedFiles { get; init; }
    public int RemovedFiles { get; init; }
    public int ModifiedFiles { get; init; }
    public int UnchangedFiles { get; init; }
} 