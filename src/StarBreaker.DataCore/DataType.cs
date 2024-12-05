namespace StarBreaker.DataCore;

public enum DataType : ushort
{
    Reference = 0x310,
    WeakPointer = 0x0210,
    StrongPointer = 0x0110,
    Class = 0x0010,
    EnumChoice = 0x000F,
    Guid = 0x000E,
    Locale = 0x000D,
    Double = 0x000C,
    Single = 0x000B,
    String = 0x000A,
    UInt64 = 0x0009,
    UInt32 = 0x0008,
    UInt16 = 0x0007,
    Byte = 0x0006,
    Int64 = 0x0005,
    Int32 = 0x0004,
    Int16 = 0x0003,
    SByte = 0x0002,
    Boolean = 0x0001
}

public static class DataTypeExtensions
{
    public static string ToStringFast(this DataType dataType) => dataType switch
    {
        DataType.Reference => "Reference",
        DataType.WeakPointer => "WeakPointer",
        DataType.StrongPointer => "StrongPointer",
        DataType.Class => "Class",
        DataType.EnumChoice => "EnumChoice",
        DataType.Guid => "Guid",
        DataType.Locale => "Locale",
        DataType.Double => "Double",
        DataType.Single => "Single",
        DataType.String => "String",
        DataType.UInt64 => "UInt64",
        DataType.UInt32 => "UInt32",
        DataType.UInt16 => "UInt16",
        DataType.Byte => "Byte",
        DataType.Int64 => "Int64",
        DataType.Int32 => "Int32",
        DataType.Int16 => "Int16",
        DataType.SByte => "SByte",
        DataType.Boolean => "Boolean",
        _ => throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null)
    };
}