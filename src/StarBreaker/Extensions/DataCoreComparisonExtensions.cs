using System.Globalization;
using StarBreaker.DataCore;

namespace StarBreaker.Extensions;

public static class DataCoreComparisonExtensions
{
    public static string GetComparisonSize(this IDataCoreComparisonNode node)
    {
        if (node is not DataCoreComparisonFileNode fileNode)
            return "";

        var leftSize = fileNode.LeftRecord?.StructSize ?? 0;
        var rightSize = fileNode.RightRecord?.StructSize ?? 0;
        
        return node.Status switch
        {
            DataCoreComparisonStatus.Added => $"+ {rightSize} bytes",
            DataCoreComparisonStatus.Removed => $"- {leftSize} bytes",
            DataCoreComparisonStatus.Modified => 
                leftSize != rightSize
                    ? $"{leftSize} → {rightSize} bytes"
                    : $"{leftSize} bytes",
            DataCoreComparisonStatus.Unchanged => $"{leftSize} bytes",
            _ => ""
        };
    }

    public static string GetComparisonDate(this IDataCoreComparisonNode node)
    {
        if (node is not DataCoreComparisonFileNode fileNode)
            return "";

        // DataCore records don't have modification dates, so we'll show the record type instead
        var leftType = GetRecordTypeName(fileNode.LeftRecord, fileNode.LeftDatabase);
        var rightType = GetRecordTypeName(fileNode.RightRecord, fileNode.RightDatabase);
        
        return node.Status switch
        {
            DataCoreComparisonStatus.Added => rightType,
            DataCoreComparisonStatus.Removed => leftType,
            DataCoreComparisonStatus.Modified when leftType != rightType => 
                $"{leftType} → {rightType}",
            _ => leftType ?? rightType ?? ""
        };
    }

    private static string GetRecordTypeName(DataCoreRecord? record, DataCoreDatabase? database)
    {
        if (record == null || database == null)
            return "";
            
        return database.StructDefinitions[record.Value.StructIndex].GetName(database);
    }

    public static string GetComparisonName(this IDataCoreComparisonNode node)
    {
        var prefix = node.Status switch
        {
            DataCoreComparisonStatus.Added => "[+] ",
            DataCoreComparisonStatus.Removed => "[-] ",
            DataCoreComparisonStatus.Modified => "[~] ",
            DataCoreComparisonStatus.Unchanged => "",
            _ => ""
        };
        
        return prefix + node.Name;
    }

    public static string GetComparisonStatus(this IDataCoreComparisonNode node)
    {
        return node.Status switch
        {
            DataCoreComparisonStatus.Added => "Added",
            DataCoreComparisonStatus.Removed => "Removed", 
            DataCoreComparisonStatus.Modified => "Modified",
            DataCoreComparisonStatus.Unchanged => "Unchanged",
            _ => ""
        };
    }

    public static ICollection<IDataCoreComparisonNode> GetComparisonChildren(this IDataCoreComparisonNode node)
    {
        return node switch
        {
            DataCoreComparisonDirectoryNode dir => dir.Children.Values,
            _ => Array.Empty<IDataCoreComparisonNode>()
        };
    }

    public static uint GetComparisonSizeValue(this IDataCoreComparisonNode node)
    {
        if (node is not DataCoreComparisonFileNode fileNode)
            return 0;

        // Use the larger of the two sizes for sorting
        var leftSize = fileNode.LeftRecord?.StructSize ?? 0;
        var rightSize = fileNode.RightRecord?.StructSize ?? 0;
        return Math.Max(leftSize, rightSize);
    }

    public static bool HasChanges(this IDataCoreComparisonNode node)
    {
        return node.Status != DataCoreComparisonStatus.Unchanged;
    }

    public static ICollection<IDataCoreComparisonNode> GetFilteredComparisonChildren(this IDataCoreComparisonNode node, bool showOnlyChanges = false)
    {
        var children = node.GetComparisonChildren();
        
        if (!showOnlyChanges)
            return children;

        return children.Where(child => 
        {
            // Always include changed files
            if (child.HasChanges())
                return true;
                
            // For directories, include them if they contain any changes (recursively)
            if (child is DataCoreComparisonDirectoryNode)
                return HasChangesRecursive(child);
                
            return false;
        }).ToArray();
    }

    private static bool HasChangesRecursive(IDataCoreComparisonNode node)
    {
        // If this node itself has changes, return true
        if (node.HasChanges())
            return true;
            
        // Check if any child has changes (recursively)
        var children = node.GetComparisonChildren();
        return children.Any(HasChangesRecursive);
    }
} 