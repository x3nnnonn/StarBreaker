#if DEBUG
namespace StarBreaker.DataCore;

/// <summary>
///     Global debug class to store the database for debugging purposes.
/// </summary>
public static class DebugGlobal
{
#pragma warning disable CA2211
    public static DataCoreDatabase Database = null!;
#pragma warning restore CA2211
}
#endif