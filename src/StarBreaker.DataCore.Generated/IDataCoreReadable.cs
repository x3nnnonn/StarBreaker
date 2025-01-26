using StarBreaker.Common;
using StarBreaker.DataCore;

namespace StarBreaker.DataCoreGenerated;

public interface IDataCoreReadable;

public interface IDataCoreReadable<out T> : IDataCoreReadable where T : class, IDataCoreReadable<T>
{
    static abstract T Read(DataCoreBinaryGenerated dataCore, ref SpanReader reader);
}