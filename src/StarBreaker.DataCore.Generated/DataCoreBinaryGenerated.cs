using StarBreaker.DataCoreGenerated;

namespace StarBreaker.DataCore;

//TODO: come up with a way of hashing the types we generated, then verify that the datacore file we're reading matches the types we have.
// if they don't match we *WILL* fail to read it. We should throw before this.
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