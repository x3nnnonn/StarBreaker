namespace StarBreaker.DataCore;

public interface IDataCoreBinary<out T>
{
    DataCoreDatabase Database { get; }
    
    T GetFromMainRecord(DataCoreRecord record);
    
    void SaveToFile(DataCoreRecord record, string path);
}