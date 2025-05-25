using System.Globalization;
using Humanizer;
using StarBreaker.P4k;
using System.Linq;

namespace StarBreaker.Extensions;

public static class P4kComparisonExtensions
{
    public static string GetComparisonSize(this IP4kComparisonNode node)
    {
        if (node is not P4kComparisonFileNode fileNode)
            return "";

        var leftSize = fileNode.LeftEntry?.UncompressedSize ?? 0;
        var rightSize = fileNode.RightEntry?.UncompressedSize ?? 0;
        
        return node.Status switch
        {
            P4kComparisonStatus.Added => $"+ {((long)rightSize).Bytes()}",
            P4kComparisonStatus.Removed => $"- {((long)leftSize).Bytes()}",
            P4kComparisonStatus.Modified => 
                $"{((long)leftSize).Bytes()} → {((long)rightSize).Bytes()}" +
                (fileNode.SizeDifference != 0 ? $" ({(fileNode.SizeDifference > 0 ? "+" : "")}{fileNode.SizeDifference.Bytes()})" : ""),
            P4kComparisonStatus.Unchanged => ((long)leftSize).Bytes().ToString(),
            _ => ""
        };
    }

    public static string GetComparisonDate(this IP4kComparisonNode node)
    {
        if (node is not P4kComparisonFileNode fileNode)
            return "";

        var leftDate = fileNode.LeftEntry?.LastModified;
        var rightDate = fileNode.RightEntry?.LastModified;
        
        return node.Status switch
        {
            P4kComparisonStatus.Added => rightDate?.ToString("s", CultureInfo.InvariantCulture) ?? "",
            P4kComparisonStatus.Removed => leftDate?.ToString("s", CultureInfo.InvariantCulture) ?? "",
            P4kComparisonStatus.Modified when leftDate != rightDate => 
                $"{leftDate?.ToString("s", CultureInfo.InvariantCulture)} → {rightDate?.ToString("s", CultureInfo.InvariantCulture)}",
            _ => leftDate?.ToString("s", CultureInfo.InvariantCulture) ?? rightDate?.ToString("s", CultureInfo.InvariantCulture) ?? ""
        };
    }

    public static string GetComparisonName(this IP4kComparisonNode node)
    {
        var prefix = node.Status switch
        {
            P4kComparisonStatus.Added => "[+] ",
            P4kComparisonStatus.Removed => "[-] ",
            P4kComparisonStatus.Modified => "[~] ",
            P4kComparisonStatus.Unchanged => "",
            _ => ""
        };
        
        return prefix + node.Name;
    }

    public static string GetComparisonStatus(this IP4kComparisonNode node)
    {
        return node.Status switch
        {
            P4kComparisonStatus.Added => "Added",
            P4kComparisonStatus.Removed => "Removed", 
            P4kComparisonStatus.Modified => "Modified",
            P4kComparisonStatus.Unchanged => "Unchanged",
            _ => ""
        };
    }

    public static ICollection<IP4kComparisonNode> GetComparisonChildren(this IP4kComparisonNode node)
    {
        return node switch
        {
            P4kComparisonDirectoryNode dir => dir.Children.Values,
            _ => Array.Empty<IP4kComparisonNode>()
        };
    }

    public static ulong GetComparisonSizeValue(this IP4kComparisonNode node)
    {
        if (node is not P4kComparisonFileNode fileNode)
            return 0;

        // Use the larger of the two sizes for sorting
        var leftSize = fileNode.LeftEntry?.UncompressedSize ?? 0;
        var rightSize = fileNode.RightEntry?.UncompressedSize ?? 0;
        return Math.Max(leftSize, rightSize);
    }

    public static bool HasChanges(this IP4kComparisonNode node)
    {
        return node.Status != P4kComparisonStatus.Unchanged;
    }

    public static ICollection<IP4kComparisonNode> GetFilteredComparisonChildren(this IP4kComparisonNode node, bool showOnlyChanges = false)
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
            if (child is P4kComparisonDirectoryNode)
                return HasChangesRecursive(child);
                
            return false;
        }).ToArray();
    }

    private static bool HasChangesRecursive(IP4kComparisonNode node)
    {
        // If this node itself has changes, return true
        if (node.HasChanges())
            return true;
            
        // Check if any child has changes (recursively)
        var children = node.GetComparisonChildren();
        return children.Any(HasChangesRecursive);
    }
} 