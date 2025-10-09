namespace StarBreaker.DataCore;

public class DataCoreTypeNode
{
    private readonly DataCoreDatabase _database;
    public int Index { get; }
    public DataCoreStructDefinition StructDefinition => _database.StructDefinitions[Index];
    public List<DataCoreTypeNode> Children { get; } = new();

    public DataCoreTypeNode(DataCoreDatabase database, int index)
    {
        _database = database;
        Index = index;
    }
}