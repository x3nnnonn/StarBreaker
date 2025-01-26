namespace StarBreaker.DataCore;

public interface IDataCoreBinary<out T>
{
    DataCoreDatabase Database { get; }
    
    T GetFromMainRecord(DataCoreRecord record, DataCoreExtractionOptions options);
    
    void SaveToFile(DataCoreRecord record, DataCoreExtractionOptions options, string path);
}