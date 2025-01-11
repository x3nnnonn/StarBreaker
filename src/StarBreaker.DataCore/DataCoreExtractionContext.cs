using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace StarBreaker.DataCore;

public sealed class DataCoreExtractionContext
{
    private readonly Dictionary<(int structIndex, int instanceIndex), int> _weakPointerIds;
    private int _nextWeakPointerId = 0;

    public Dictionary<(int structIndex, int instanceIndex), XElement> Elements { get; }

    public string FileName { get; }
    public bool ShouldWriteMetadata { get; }
    public bool ShouldWriteNulls { get; }

    public DataCoreExtractionContext(string fileName, bool shouldWriteMetadata = false, bool shouldWriteNulls = false)
    {
        FileName = fileName;
        ShouldWriteMetadata = shouldWriteMetadata;
        ShouldWriteNulls = shouldWriteNulls;
        Elements = [];
        _weakPointerIds = [];
    }

    public int AddWeakPointer(int structIndex, int instanceIndex)
    {
        ref var id = ref CollectionsMarshal.GetValueRefOrAddDefault(_weakPointerIds, (structIndex, instanceIndex), out var existed);

        if (!existed)
            id = _nextWeakPointerId++;

        return id;
    }

    public int GetWeakPointerId(int structIndex, int instanceIndex) => _weakPointerIds[(structIndex, instanceIndex)];

    public IEnumerable<(int structIndex, int instanceIndex)> GetWeakPointers() => _weakPointerIds.Keys;
}