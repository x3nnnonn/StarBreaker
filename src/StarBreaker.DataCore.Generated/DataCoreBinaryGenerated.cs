using StarBreaker.DataCoreGenerated;

namespace StarBreaker.DataCore;

public sealed class DataCoreBinaryGenerated : IDataCoreBinary<IDataCoreReadable>
{
    public DataCoreDatabase Database { get; }
    
    public DataCoreBinaryGenerated(DataCoreDatabase database)
    {
        Database = database;
    }
    
    public IDataCoreReadable GetFromMainRecord(DataCoreRecord record)
    {
        var data = TypeMap.ReadFromRecord(Database, record.StructIndex, record.InstanceIndex);
        
        if (data == null)
            throw new InvalidOperationException($"Failed to read data from record {record}");
        
        return data;
    }

    public void SaveToFile(DataCoreRecord record, string path)
    {
        throw new NotImplementedException();
    }
}