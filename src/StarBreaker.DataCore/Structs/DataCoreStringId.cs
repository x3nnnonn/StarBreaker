using System.Diagnostics;
using System.Runtime.InteropServices;

namespace StarBreaker.DataCore;

#if DEBUG
[DebuggerDisplay("{Value}")]
#endif
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct DataCoreStringId
{
    public readonly int Id;

    public string ToString(DataCoreDatabase db) => db.GetString(this);
    
#if DEBUG
    public string Value => DebugGlobal.Database.GetString(this);
#endif
}