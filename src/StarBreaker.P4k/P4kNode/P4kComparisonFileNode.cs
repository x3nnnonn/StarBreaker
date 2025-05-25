using System.Diagnostics;

namespace StarBreaker.P4k;

[DebuggerDisplay("{FullPath} ({Status})")]
public sealed class P4kComparisonFileNode : IP4kComparisonNode
{
    public IP4kComparisonNode? Parent { get; }
    public string Name { get; }
    public string FullPath { get; }
    public P4kComparisonStatus Status { get; }
    
    // File information from left P4K (null if file doesn't exist in left)
    public ZipEntry? LeftEntry { get; }
    
    // File information from right P4K (null if file doesn't exist in right)
    public ZipEntry? RightEntry { get; }
    
    // Size difference (right - left, 0 if unchanged or only in one P4K)
    public long SizeDifference => 
        (long)(RightEntry?.UncompressedSize ?? 0) - (long)(LeftEntry?.UncompressedSize ?? 0);
    
    public P4kComparisonFileNode(
        string name, 
        string fullPath, 
        P4kComparisonStatus status,
        ZipEntry? leftEntry,
        ZipEntry? rightEntry,
        IP4kComparisonNode? parent)
    {
        Name = name;
        FullPath = fullPath;
        Status = status;
        LeftEntry = leftEntry;
        RightEntry = rightEntry;
        Parent = parent;
    }
} 