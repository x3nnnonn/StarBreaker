using StarBreaker.Common;

namespace StarBreaker.DataCore;

public sealed class DataCoreBinaryObjects
{
    public DataCoreDatabase Database { get; }

    public DataCoreBinaryObjects(DataCoreDatabase db)
    {
        Database = db;
    }

    private DataCoreObject GetFromStruct(string name, int structIndex, ref SpanReader reader, DataCoreExtractionContext<DataCoreObject> context)
    {
        var properties = Database.GetProperties(structIndex);

        var arr = new IDataCoreObject[properties.Length];

        for (var i = 0; i < properties.Length; i++)
        {
            var prop = properties[i];
            var propName = prop.GetName(Database);

            arr[i] = prop.ConversionType switch
            {
                ConversionType.Attribute => GetAttribute(propName, prop, ref reader, context),
                _ => GetArray(propName, prop, ref reader, context)
            };
        }

        return new DataCoreObject(name, arr);
    }

    public IDataCoreObject GetArray(string propName, DataCorePropertyDefinition prop, ref SpanReader reader, DataCoreExtractionContext<DataCoreObject> context)
    {
        var count = reader.ReadInt32();
        var firstIndex = reader.ReadInt32();

        var structName = Database.StructDefinitions[prop.StructIndex].GetName(Database);

        return prop.DataType switch
        {
            DataType.Boolean => new DataCoreValue<Memory<bool>>(propName, Database.BooleanValues.AsMemory(firstIndex, count)),
            DataType.Byte => new DataCoreValue<Memory<byte>>(propName, Database.UInt8Values.AsMemory(firstIndex, count)),
            DataType.SByte => new DataCoreValue<Memory<sbyte>>(propName, Database.Int8Values.AsMemory(firstIndex, count)),
            DataType.Int16 => new DataCoreValue<Memory<short>>(propName, Database.Int16Values.AsMemory(firstIndex, count)),
            DataType.UInt16 => new DataCoreValue<Memory<ushort>>(propName, Database.UInt16Values.AsMemory(firstIndex, count)),
            DataType.Int32 => new DataCoreValue<Memory<int>>(propName, Database.Int32Values.AsMemory(firstIndex, count)),
            DataType.UInt32 => new DataCoreValue<Memory<uint>>(propName, Database.UInt32Values.AsMemory(firstIndex, count)),
            DataType.Int64 => new DataCoreValue<Memory<long>>(propName, Database.Int64Values.AsMemory(firstIndex, count)),
            DataType.UInt64 => new DataCoreValue<Memory<ulong>>(propName, Database.UInt64Values.AsMemory(firstIndex, count)),
            DataType.Single => new DataCoreValue<Memory<float>>(propName, Database.SingleValues.AsMemory(firstIndex, count)),
            DataType.Double => new DataCoreValue<Memory<double>>(propName, Database.DoubleValues.AsMemory(firstIndex, count)),
            DataType.String => new DataCoreValue<Memory<DataCoreStringId>>(propName, Database.StringIdValues.AsMemory(firstIndex, count)),
            DataType.Guid => new DataCoreValue<Memory<CigGuid>>(propName, Database.GuidValues.AsMemory(firstIndex, count)),
            DataType.Locale => new DataCoreValue<Memory<DataCoreStringId>>(propName, Database.LocaleValues.AsMemory(firstIndex, count)),
            DataType.EnumChoice => new DataCoreValue<Memory<DataCoreStringId>>(propName, Database.EnumValues.AsMemory(firstIndex, count)),

            DataType.Reference => new DataCoreValue<IDataCoreObject[]>(propName, GetFromReferences(structName, firstIndex, count, context)),
            DataType.WeakPointer => new DataCoreValue<IDataCoreObject[]>(propName, GetFromWeakPointers(structName, firstIndex, count, context)),
            DataType.StrongPointer => new DataCoreValue<IDataCoreObject[]>(propName, GetFromPointers(structName, firstIndex, count, context)),
            DataType.Class => new DataCoreValue<IDataCoreObject[]>(propName, GetFromInstances(structName, prop.StructIndex, firstIndex, count, context)),
            _ => throw new InvalidOperationException(nameof(DataType))
        };
    }

    private IDataCoreObject[] GetFromReferences(string structName, int start, int count, DataCoreExtractionContext<DataCoreObject> context)
    {
        var arr = new IDataCoreObject[count];

        for (var i = 0; i < count; i++)
        {
            arr[i] = GetFromReference(structName, Database.ReferenceValues[start + i], context, true);
        }

        return arr;
    }

    private IDataCoreObject[] GetFromWeakPointers(string structName, int start, int count, DataCoreExtractionContext<DataCoreObject> context)
    {
        var arr = new IDataCoreObject[count];

        for (var i = 0; i < count; i++)
        {
            arr[i] = GetWeakPointer(structName, Database.WeakValues[start + i], context, true);
        }

        return arr;
    }

    private IDataCoreObject[] GetFromPointers(string structName, int start, int count, DataCoreExtractionContext<DataCoreObject> context)
    {
        var arr = new IDataCoreObject[count];

        for (var i = 0; i < count; i++)
        {
            arr[i] = GetFromPointer(structName, Database.StrongValues[start + i], context, true);
        }

        return arr;
    }

    private IDataCoreObject[] GetFromInstances(string structName, int structIndex, int i, int count, DataCoreExtractionContext<DataCoreObject> context)
    {
        var arr = new IDataCoreObject[count];

        for (var j = 0; j < count; j++)
        {
            arr[j] = GetFromInstance(structName, structIndex, i + j, context, true);
        }

        return arr;
    }

    private IDataCoreObject GetAttribute(string propertyName, DataCorePropertyDefinition prop, ref SpanReader reader, DataCoreExtractionContext<DataCoreObject> context)
    {
        return prop.DataType switch
        {
            DataType.Reference => GetFromReference(propertyName, reader.Read<DataCoreReference>(), context),
            DataType.WeakPointer => GetWeakPointer(propertyName, reader.Read<DataCorePointer>(), context),
            DataType.StrongPointer => GetFromPointer(propertyName, reader.Read<DataCorePointer>(), context),
            DataType.Class => GetFromStruct(propertyName, prop.StructIndex, ref reader, context),

            DataType.EnumChoice => new DataCoreValue<DataCoreStringId>(propertyName, reader.Read<DataCoreStringId>()),
            DataType.Guid => new DataCoreValue<CigGuid>(propertyName, reader.Read<CigGuid>()),
            DataType.Locale => new DataCoreValue<DataCoreStringId>(propertyName, reader.Read<DataCoreStringId>()),
            DataType.Double => new DataCoreValue<double>(propertyName, reader.ReadDouble()),
            DataType.Single => new DataCoreValue<float>(propertyName, reader.ReadSingle()),
            DataType.String => new DataCoreValue<DataCoreStringId>(propertyName, reader.Read<DataCoreStringId>()),
            DataType.UInt64 => new DataCoreValue<ulong>(propertyName, reader.ReadUInt64()),
            DataType.UInt32 => new DataCoreValue<uint>(propertyName, reader.ReadUInt32()),
            DataType.UInt16 => new DataCoreValue<ushort>(propertyName, reader.ReadUInt16()),
            DataType.Byte => new DataCoreValue<byte>(propertyName, reader.ReadByte()),
            DataType.Int64 => new DataCoreValue<long>(propertyName, reader.ReadInt64()),
            DataType.Int32 => new DataCoreValue<int>(propertyName, reader.ReadInt32()),
            DataType.Int16 => new DataCoreValue<short>(propertyName, reader.ReadInt16()),
            DataType.SByte => new DataCoreValue<sbyte>(propertyName, reader.ReadSByte()),
            DataType.Boolean => new DataCoreValue<bool>(propertyName, reader.ReadBoolean()),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private IDataCoreObject GetFromReference(string name, DataCoreReference reference, DataCoreExtractionContext<DataCoreObject> context, bool overrideName = false)
    {
        if (reference.InstanceIndex == -1 || reference.RecordId == CigGuid.Empty)
            return new DataCoreValue<object?>(name, null);

        var record = Database.GetRecord(reference.RecordId);

        if (Database.MainRecords.Contains(reference.RecordId))
        {
            //if we're referencing a full on file, just add a small mention to it

            var arr = new IDataCoreObject[3];
            arr[0] = new DataCoreValue<CigGuid>("__recordId", reference.RecordId);
            arr[1] = new DataCoreValue<string>("__recordPath", DataCoreUtils.ComputeRelativePath(record.GetFileName(Database), context.FileName));
            arr[2] = new DataCoreValue<string>("__typeName", Database.StructDefinitions[record.StructIndex].GetName(Database));
            return new DataCoreObject(name, arr);
        }

        return GetFromInstance(name, record.StructIndex, record.InstanceIndex, context, overrideName);
    }

    public IDataCoreObject GetFromMainRecord(DataCoreRecord record, DataCoreExtractionContext<DataCoreObject> context)
    {
        if (!Database.MainRecords.Contains(record.Id))
            throw new InvalidOperationException("Can only extract main records");

        var recordName = record.GetName(Database)
            .Replace("'", "_")
            .Replace(" ", "_")
            .Replace("/", "_")
            .Replace("&", "_");

        var element = GetFromInstance(recordName, record.StructIndex, record.InstanceIndex, context);

        //add weak pointers ids, so we can actually see what a weak pointer is pointing at
        // foreach (var weakPtr in context.GetWeakPointers())
        // {
        //     var pointedAtElement = context.Elements[(weakPtr.structIndex, weakPtr.instanceIndex)];
        //
        //     pointedAtElement.Add(new DataCoreValue<int>("__weakPointerId", context.GetWeakPointerId(weakPtr.structIndex, weakPtr.instanceIndex)));
        // }

        return element;
    }

    private IDataCoreObject GetFromPointer(string name, DataCorePointer pointer, DataCoreExtractionContext<DataCoreObject> context, bool overrideName = false)
    {
        return GetFromInstance(name, pointer.StructIndex, pointer.InstanceIndex, context, overrideName);
    }
    
    private IDataCoreObject GetFromInstance(string name, int structIndex, int instanceIndex, DataCoreExtractionContext<DataCoreObject> context, bool overrideName = false)
    {
        if (structIndex == -1 || instanceIndex == -1)
            return new DataCoreValue<object?>(name, null);

        var reader = Database.GetReader(structIndex, instanceIndex);

        // We override the name in some cases.
        // The name we get from our parent can either be the name of a property (useful),
        // Or the base datatype of our struct (not really useful, we'd rather have our actual type name in that case).
        // When that happens, we override our parent's name with our actual type name.

        //This seems to only be required when we're in an array, since the array can be of the base type.
        // When we're a property/field, the name is custom (and usually more informative), and the type name is written separately
        if (overrideName)
            name = Database.StructDefinitions[structIndex].GetName(Database);

        var element = GetFromStruct(name, structIndex, ref reader, context);

        context.Elements[(structIndex, instanceIndex)] = element;

        return element;
    }

    private IDataCoreObject GetWeakPointer(string name, DataCorePointer pointer, DataCoreExtractionContext<DataCoreObject> context, bool overrideName = false)
    {
        if (pointer.InstanceIndex == -1 || pointer.StructIndex == -1)
            return new DataCoreValue<object?>(name, null);

        var pointerId = context.AddWeakPointer(pointer.StructIndex, pointer.InstanceIndex);

        var weakPointer = new DataCoreObject(Database.StructDefinitions[pointer.StructIndex].GetName(Database),
        [
            new DataCoreValue<int>("__weakPointerId", pointerId),
            new DataCoreValue<string>("__pointsTo_Name", name),
            new DataCoreValue<string>("__pointsTo_TypeName", Database.StructDefinitions[pointer.StructIndex].GetName(Database))
        ]);

        return weakPointer;
    }
}