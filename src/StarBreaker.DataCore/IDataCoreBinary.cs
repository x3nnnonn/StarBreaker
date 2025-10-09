namespace StarBreaker.DataCore;

public interface IDataCoreBinary<out T>
{
    DataCoreDatabase Database { get; }
    
    T GetFromMainRecord(DataCoreRecord record);
    
    void SaveRecordToFile(DataCoreRecord record, string path);
    
    void SaveStructToFile(int structIndex, string path);
    
    void SaveEnumToFile(int enumIndex, string path);
}