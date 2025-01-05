namespace StarBreaker.DataCore;

/// <summary>
///     The strategy to use when resolving repeated references in the data core.
/// </summary>
public enum DataCoreRepeatedReferenceResolutionStrategy
{
    /// <summary>
    ///     Each instance will be written once per file. Subsequent references will be written as a reference to the original instance.
    /// </summary>
    PerFile,

    /// <summary>
    ///     Each instance will be written once per node. References to the original instance will be used when they are children of the original.
    ///     Otherwise, the instance will be written again.
    /// </summary>
    PerNode,
}

public sealed class DataCoreExtractionContext
{
    private readonly HashSet<(int structIndex, int instanceIndex)> _hashSet;
    private readonly Stack<(int structIndex, int instanceIndex)> _stack;

    public string FileName { get; }
    public DataCoreRepeatedReferenceResolutionStrategy Strategy { get; }

    public DataCoreExtractionContext(string fileName, DataCoreRepeatedReferenceResolutionStrategy strategy)
    {
        _hashSet = [];
        _stack = [];
        FileName = fileName;
        Strategy = strategy;
    }

    public bool AlreadyWroteInstance(int structIndex, int instanceIndex)
    {
        return Strategy switch
        {
            DataCoreRepeatedReferenceResolutionStrategy.PerFile => _hashSet.Contains((structIndex, instanceIndex)),
            DataCoreRepeatedReferenceResolutionStrategy.PerNode => _stack.Contains((structIndex, instanceIndex)),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public void Push(int structIndex, int instanceIndex)
    {
        switch (Strategy)
        {
            case DataCoreRepeatedReferenceResolutionStrategy.PerFile:
                _hashSet.Add((structIndex, instanceIndex));
                break;
            case DataCoreRepeatedReferenceResolutionStrategy.PerNode:
                _stack.Push((structIndex, instanceIndex));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void Pop()
    {
        if (Strategy == DataCoreRepeatedReferenceResolutionStrategy.PerNode)
            _stack.Pop();

        // Otherwise, do nothing.
    }
}

