using StarBreaker.Common;

namespace StarBreaker.DataCore;

public interface IDataCoreReadable;

public interface IDataCoreReadable<out T> : IDataCoreReadable where T : class, IDataCoreReadable<T>
{
    static abstract T Read(DataCoreDatabase db, ref SpanReader reader);
}