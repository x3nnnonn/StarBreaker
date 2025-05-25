namespace StarBreaker.P4k;

public enum P4kComparisonStatus
{
    Unchanged,
    Added,
    Removed,
    Modified
}

public interface IP4kComparisonNode
{
    IP4kComparisonNode? Parent { get; }
    string Name { get; }
    string FullPath { get; }
    P4kComparisonStatus Status { get; }
} 