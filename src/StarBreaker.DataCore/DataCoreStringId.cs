using System.Diagnostics;
using System.Runtime.InteropServices;

namespace StarBreaker.DataCore;

[DebuggerDisplay("{Value}")]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct DataCoreStringId
{
    public readonly int Id;

    public string ToString(DataCoreDatabase db) => db.GetString(this);
    
#if DEBUG
    public string Value => DebugGlobal.Database.GetString(this);
#endif
}

[DebuggerDisplay("{Value}")]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct DataCoreStringId2
{
    public readonly int Id;

    public string ToString(DataCoreDatabase db) => db.GetString2(this);
    
#if DEBUG
    public string Value => DebugGlobal.Database.GetString2(this);
#endif
}