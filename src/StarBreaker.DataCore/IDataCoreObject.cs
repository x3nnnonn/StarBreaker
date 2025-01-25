using System.Diagnostics;

namespace StarBreaker.DataCore;

public interface IDataCoreObject
{
    string Name { get; }
}

[DebuggerDisplay("{Name} = {Children.Length}")]
public sealed class DataCoreObject : IDataCoreObject
{
    public string Name { get; }
    public IDataCoreObject[] Children { get; }

    public DataCoreObject(string name, IDataCoreObject[] children)
    {
        Name = name;
        Children = children;
    }
}

[DebuggerDisplay("{Name} = {Value}")]
public sealed class DataCoreValue<T> : IDataCoreObject
{
    public string Name { get; }
    public T Value { get; }

    public DataCoreValue(string name, T value)
    {
        Name = name;
        Value = value;
    }
}