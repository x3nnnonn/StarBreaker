using System.Runtime.CompilerServices;
using StarBreaker.Common;

namespace StarBreaker.DataCore;

public static class EnumExtensions
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

    public static string ToStringFast(this ConversionType conversionType) => conversionType switch
    {
        ConversionType.Attribute => "Attribute",
        ConversionType.ClassArray => "ClassArray",
        ConversionType.ComplexArray => "ComplexArray",
        ConversionType.SimpleArray => "SimpleArray",
        _ => throw new ArgumentOutOfRangeException(nameof(conversionType), conversionType, null)
    };
    
    public static int GetSize(this DataType dataType) => dataType switch
    {
        DataType.EnumChoice => Unsafe.SizeOf<DataCoreStringId>(),
        DataType.Guid => Unsafe.SizeOf<CigGuid>(),
        DataType.Locale => Unsafe.SizeOf<DataCoreStringId>(),
        DataType.Double => sizeof(double),
        DataType.Single => sizeof(float),
        DataType.String => Unsafe.SizeOf<DataCoreStringId>(),
        DataType.UInt64 => sizeof(ulong),
        DataType.UInt32 => sizeof(uint),
        DataType.UInt16 => sizeof(ushort),
        DataType.Byte => sizeof(byte),
        DataType.Int64 => sizeof(long),
        DataType.Int32 => sizeof(int),
        DataType.Int16 => sizeof(short),
        DataType.SByte => sizeof(sbyte),
        DataType.Boolean => sizeof(bool),
        _ => throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null)
    };
}