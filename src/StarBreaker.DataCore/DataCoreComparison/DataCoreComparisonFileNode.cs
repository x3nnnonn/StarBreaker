using System.Diagnostics;

namespace StarBreaker.DataCore;

[DebuggerDisplay("{FullPath} ({Status})")]
public sealed class DataCoreComparisonFileNode : IDataCoreComparisonNode
{
    public IDataCoreComparisonNode? Parent { get; }
    public string Name { get; }
    public string FullPath { get; }
    public DataCoreComparisonStatus Status { get; }
    
    // Record information from left DataCore (null if record doesn't exist in left)
    public DataCoreRecord? LeftRecord { get; }
    
    // Record information from right DataCore (null if record doesn't exist in right)
    public DataCoreRecord? RightRecord { get; }
    
    // DataCore databases for content comparison
    public DataCoreDatabase? LeftDatabase { get; }
    public DataCoreDatabase? RightDatabase { get; }
    
    public DataCoreComparisonFileNode(
        string name, 
        string fullPath, 
        DataCoreComparisonStatus status,
        DataCoreRecord? leftRecord,
        DataCoreRecord? rightRecord,
        DataCoreDatabase? leftDatabase,
        DataCoreDatabase? rightDatabase,
        IDataCoreComparisonNode? parent)
    {
        Name = name;
        FullPath = fullPath;
        Status = status;
        LeftRecord = leftRecord;
        RightRecord = rightRecord;
        LeftDatabase = leftDatabase;
        RightDatabase = rightDatabase;
        Parent = parent;
    }
} 