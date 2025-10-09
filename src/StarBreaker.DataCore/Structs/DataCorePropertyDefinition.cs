using System.Runtime.InteropServices;

namespace StarBreaker.DataCore;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct DataCorePropertyDefinition
{
    private readonly DataCoreStringId2 NameOffset;
    public readonly ushort StructIndex;
    public readonly DataType DataType;
    public readonly ConversionType ConversionType;
    private readonly ushort _padding;

    public string GetName(DataCoreDatabase db) => db.GetString2(NameOffset);

    public string GetTypeString(DataCoreDatabase db)
    {
        var typeString = GetScalarTypeString(db);

        return ConversionType switch
        {
            ConversionType.Attribute => typeString,
            _ => $"{typeString}[]"
        };
    }

    private string GetScalarTypeString(DataCoreDatabase db) => DataType switch
    {
        DataType.Boolean => "bool",
        DataType.Byte => "byte",
        DataType.SByte => "sbyte",
        DataType.Int16 => "short",
        DataType.UInt16 => "ushort",
        DataType.Int32 => "int",
        DataType.UInt32 => "uint",
        DataType.Int64 => "long",
        DataType.UInt64 => "ulong",
        DataType.Single => "float",
        DataType.Double => "double",
        DataType.Guid => "CigGuid",
        DataType.Locale => "string",
        DataType.String => "string",

        DataType.EnumChoice => db.EnumDefinitions[StructIndex].GetName(db),
        DataType.Reference => db.StructDefinitions[StructIndex].GetName(db),
        DataType.StrongPointer => db.StructDefinitions[StructIndex].GetName(db),
        DataType.Class => db.StructDefinitions[StructIndex].GetName(db),
        DataType.WeakPointer => db.StructDefinitions[StructIndex].GetName(db),
        _ => throw new ArgumentOutOfRangeException()
    };

#if DEBUG
    public string Name => DebugGlobal.Database.GetString2(NameOffset);
#endif
}