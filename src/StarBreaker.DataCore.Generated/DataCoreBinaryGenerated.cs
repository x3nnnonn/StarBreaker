namespace StarBreaker.DataCore;

public sealed class DataCoreBinaryGenerated : IDataCoreBinary<IDataCoreReadable>
{
    public DataCoreDatabase Database { get; }
    public IDataCoreReadable GetFromMainRecord(DataCoreRecord record)
    {
        throw new NotImplementedException();
    }

    public void SaveToFile(DataCoreRecord record, string path)
    {
        throw new NotImplementedException();
    }
}