using System.Runtime.InteropServices;

namespace StarBreaker.Forge;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct DataForgeStringId
{
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    public readonly int Id;
    
#if DEBUG
    public string Name => DebugGlobal.Database.GetString(this);
    public override string ToString() => Name;
#endif

    public DataForgeStringId(int id)
    {
        Id = id;
    }
}