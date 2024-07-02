#if DEBUG
namespace StarBreaker.Forge;

/// <summary>
///     Global debug class to store the database for debugging purposes.
/// </summary>
public static class DebugGlobal
{
#pragma warning disable CA2211
    public static Database Database = null!;
#pragma warning restore CA2211
}
#endif