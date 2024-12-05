using System.Runtime.InteropServices;

namespace StarBreaker.DataCore;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct DataCoreStringId
{
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    public readonly int Id;
    
#if DEBUG
    public string Name => DebugGlobal.Database.GetString(this);
    public override string ToString() => Name;
#endif
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct DataCoreStringId2
{
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    public readonly int Id;
    
#if DEBUG
    public string Name => DebugGlobal.Database.GetString2(this);
    public override string ToString() => Name;
#endif
}