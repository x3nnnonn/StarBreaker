namespace StarBreaker.DataCore;

public enum DataCoreComparisonStatus
{
    Unchanged,
    Added,
    Removed,
    Modified
}

public interface IDataCoreComparisonNode
{
    IDataCoreComparisonNode? Parent { get; }
    string Name { get; }
    string FullPath { get; }
    DataCoreComparisonStatus Status { get; }
} 