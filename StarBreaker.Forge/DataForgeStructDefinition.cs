using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace StarBreaker.Forge;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct DataForgeStructDefinition
{
    public readonly DataForgeStringId NameOffset;
    public readonly uint ParentTypeIndex;
    public readonly ushort AttributeCount;
    public readonly ushort FirstAttributeIndex;
    public readonly uint NodeType;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CalculateSize(Database db, ReadOnlySpan<DataForgeStructDefinition> structs)
    {
        var size = 0;

        foreach (ref readonly var attribute in EnumerateProperties(db))
        {
            if (attribute.ConversionType == ConversionType.Attribute)
            {
                size += attribute.DataType switch
                {
                    DataType.Reference => Unsafe.SizeOf<DataForgeReference>(),
                    DataType.WeakPointer => Unsafe.SizeOf<uint>() * 2,
                    DataType.StrongPointer => Unsafe.SizeOf<uint>() * 2,
                    DataType.EnumChoice => Unsafe.SizeOf<DataForgeStringId>(),
                    DataType.Guid => Unsafe.SizeOf<CigGuid>(),
                    DataType.Locale => Unsafe.SizeOf<DataForgeStringId>(),
                    DataType.Double => Unsafe.SizeOf<double>(),
                    DataType.Single => Unsafe.SizeOf<float>(),
                    DataType.String => Unsafe.SizeOf<DataForgeStringId>(),
                    DataType.UInt64 => Unsafe.SizeOf<ulong>(),
                    DataType.UInt32 => Unsafe.SizeOf<uint>(),
                    DataType.UInt16 => Unsafe.SizeOf<ushort>(),
                    DataType.Byte => Unsafe.SizeOf<byte>(),
                    DataType.Int64 => Unsafe.SizeOf<long>(),
                    DataType.Int32 => Unsafe.SizeOf<int>(),
                    DataType.Int16 => Unsafe.SizeOf<short>(),
                    DataType.SByte => Unsafe.SizeOf<sbyte>(),
                    DataType.Boolean => Unsafe.SizeOf<byte>(),
                    DataType.Class => structs[attribute.StructIndex].CalculateSize(db, structs),
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
            else
            {
                //array count + array offset
                size += sizeof(int) * 2;
            }
        }

        return size;
    }

    public DataForgePropertyEnumerator EnumerateProperties(Database database) => new(this, database);

#if DEBUG
    public string PropsAsString => string.Join("\n", Properties);
    public List<DataForgePropertyDefinition> Properties
    {
        get
        {
            var _properties = new List<DataForgePropertyDefinition>();
            
            foreach (ref readonly var prop in EnumerateProperties(DebugGlobal.Database))
            {
                _properties.Add(prop);
            }

            return _properties;
        }
    }
#endif
}