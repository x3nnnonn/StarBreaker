namespace StarBreaker.DataCore;

public sealed class DataCoreExtractionContext
{
    public HashSet<(int structIndex, int instanceIndex)> Tracker { get; }
    public string FileName { get; }

    public DataCoreExtractionContext(string fileName)
    {
        Tracker = [];
        FileName = fileName;
    }
}