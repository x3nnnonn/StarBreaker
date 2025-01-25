using System.Globalization;
using System.Xml.Linq;
using StarBreaker.Common;

namespace StarBreaker.DataCore;

public sealed class DataCoreBinary
{
    public DataCoreDatabase Database { get; }

    public DataCoreBinary(DataCoreDatabase db)
    {
        Database = db;
    }

    private XElement GetFromStruct(string name, int structIndex, ref SpanReader reader, DataCoreExtractionContext<XElement> context)
    {
        var node = new XElement(name);

        foreach (var prop in Database.GetProperties(structIndex))
        {
            var propName = prop.GetName(Database);
            node.Add(prop.ConversionType switch
            {
                ConversionType.Attribute => GetAttribute(propName, prop, ref reader, context),
                _ => GetArray(propName, prop, ref reader, context)?.WithAttribute("__dataType", prop.ConversionType.ToStringFast(), context.Options.ShouldWriteEnumMetadata)
            });
        }

        return node;
    }

    private XElement? GetArray(string propName, DataCorePropertyDefinition prop, ref SpanReader reader, DataCoreExtractionContext<XElement> context)
    {
        var count = reader.ReadInt32();
        var firstIndex = reader.ReadInt32();

        if (count == 0 && context.Options.ShouldSkipEmptyArrays)
            return null;

        var arrayNode = new XElement(propName).WithAttribute("__arrayLength", count.ToString(CultureInfo.InvariantCulture));

        var structName = Database.StructDefinitions[prop.StructIndex].GetName(Database);
        var dataTypeName = prop.DataType.ToStringFast();

        for (var i = firstIndex; i < firstIndex + count; i++)
        {
            arrayNode.Add(prop.DataType switch
            {
                DataType.Reference => GetFromReference(structName, Database.ReferenceValues[i], context, true).WithAttribute("__dataType", "ArrStrong", context.Options.ShouldWriteEnumMetadata),
                DataType.WeakPointer => GetWeakPointer(structName, Database.WeakValues[i], context, true).WithAttribute("__dataType", "ArrWeak", context.Options.ShouldWriteEnumMetadata),
                DataType.StrongPointer => GetFromPointer(structName, Database.StrongValues[i], context, true).WithAttribute("__dataType", "ArrStrong", context.Options.ShouldWriteEnumMetadata),
                DataType.Class => GetFromInstance(structName, prop.StructIndex, i, context, true).WithAttribute("__dataType", "ArrClass", context.Options.ShouldWriteEnumMetadata),

                DataType.EnumChoice => new XElement(Database.EnumDefinitions[prop.StructIndex].GetName(Database), Database.EnumValues[i].ToString(Database)),
                DataType.Guid => new XElement(dataTypeName, Database.GuidValues[i].ToString()),
                DataType.Locale => new XElement(dataTypeName, Database.LocaleValues[i].ToString(Database)),
                DataType.Double => new XElement(dataTypeName, Database.DoubleValues[i].ToString(CultureInfo.InvariantCulture)),
                DataType.Single => new XElement(dataTypeName, Database.SingleValues[i].ToString(CultureInfo.InvariantCulture)),
                DataType.String => new XElement(dataTypeName, Database.StringIdValues[i].ToString(Database)),
                DataType.UInt64 => new XElement(dataTypeName, Database.UInt64Values[i].ToString(CultureInfo.InvariantCulture)),
                DataType.UInt32 => new XElement(dataTypeName, Database.UInt32Values[i].ToString(CultureInfo.InvariantCulture)),
                DataType.UInt16 => new XElement(dataTypeName, Database.UInt16Values[i].ToString(CultureInfo.InvariantCulture)),
                DataType.Byte => new XElement(dataTypeName, Database.UInt8Values[i].ToString(CultureInfo.InvariantCulture)),
                DataType.Int64 => new XElement(dataTypeName, Database.Int64Values[i].ToString(CultureInfo.InvariantCulture)),
                DataType.Int32 => new XElement(dataTypeName, Database.Int32Values[i].ToString(CultureInfo.InvariantCulture)),
                DataType.Int16 => new XElement(dataTypeName, Database.Int16Values[i].ToString(CultureInfo.InvariantCulture)),
                DataType.SByte => new XElement(dataTypeName, Database.Int8Values[i].ToString(CultureInfo.InvariantCulture)),
                DataType.Boolean => new XElement(dataTypeName, Database.BooleanValues[i].ToString(CultureInfo.InvariantCulture)),
                _ => throw new InvalidOperationException(nameof(DataType))
            });
        }

        WriteMetadata(arrayNode, prop.StructIndex, firstIndex, context);
        WriteTypeNames(arrayNode, prop.StructIndex, context);

        return arrayNode;
    }

    private XElement GetAttribute(string propertyName, DataCorePropertyDefinition prop, ref SpanReader reader, DataCoreExtractionContext<XElement> context)
    {
        return prop.DataType switch
        {
            DataType.Reference => GetFromReference(propertyName, reader.Read<DataCoreReference>(), context).WithAttribute("__dataType", "AttReference", context.Options.ShouldWriteEnumMetadata),
            DataType.WeakPointer => GetWeakPointer(propertyName, reader.Read<DataCorePointer>(), context).WithAttribute("__dataType", "AttWeak", context.Options.ShouldWriteEnumMetadata),
            DataType.StrongPointer => GetFromPointer(propertyName, reader.Read<DataCorePointer>(), context).WithAttribute("__dataType", "AttStrong", context.Options.ShouldWriteEnumMetadata),
            DataType.Class => GetFromStruct(propertyName, prop.StructIndex, ref reader, context).WithAttribute("__dataType", "AttClass", context.Options.ShouldWriteEnumMetadata),

            DataType.EnumChoice => new XElement(propertyName, reader.Read<DataCoreStringId>().ToString(Database)),
            DataType.Guid => new XElement(propertyName, reader.Read<CigGuid>().ToString()),
            DataType.Locale => new XElement(propertyName, reader.Read<DataCoreStringId>().ToString(Database)),
            DataType.Double => new XElement(propertyName, reader.ReadDouble().ToString(CultureInfo.InvariantCulture)),
            DataType.Single => new XElement(propertyName, reader.ReadSingle().ToString(CultureInfo.InvariantCulture)),
            DataType.String => new XElement(propertyName, reader.Read<DataCoreStringId>().ToString(Database)),
            DataType.UInt64 => new XElement(propertyName, reader.ReadUInt64().ToString(CultureInfo.InvariantCulture)),
            DataType.UInt32 => new XElement(propertyName, reader.ReadUInt32().ToString(CultureInfo.InvariantCulture)),
            DataType.UInt16 => new XElement(propertyName, reader.ReadUInt16().ToString(CultureInfo.InvariantCulture)),
            DataType.Byte => new XElement(propertyName, reader.ReadByte().ToString(CultureInfo.InvariantCulture)),
            DataType.Int64 => new XElement(propertyName, reader.ReadInt64().ToString(CultureInfo.InvariantCulture)),
            DataType.Int32 => new XElement(propertyName, reader.ReadInt32().ToString(CultureInfo.InvariantCulture)),
            DataType.Int16 => new XElement(propertyName, reader.ReadInt16().ToString(CultureInfo.InvariantCulture)),
            DataType.SByte => new XElement(propertyName, reader.ReadSByte().ToString(CultureInfo.InvariantCulture)),
            DataType.Boolean => new XElement(propertyName, reader.ReadBoolean().ToString(CultureInfo.InvariantCulture)),
            _ => throw new ArgumentOutOfRangeException(nameof(prop))
        };
    }

    private XElement GetFromReference(string name, DataCoreReference reference, DataCoreExtractionContext<XElement> context, bool overrideName = false)
    {
        if (reference.InstanceIndex == -1 || reference.RecordId == CigGuid.Empty)
            return GetNull(name, context, -1, reference.InstanceIndex);

        var record = Database.GetRecord(reference.RecordId);

        if (Database.MainRecords.Contains(reference.RecordId))
        {
            //if we're referencing a full on file, just add a small mention to it
            var fileReferenceNode = new XElement(name);
            fileReferenceNode.Add(new XAttribute("__recordId", reference.RecordId.ToString()));
            fileReferenceNode.Add(new XAttribute("__recordPath", DataCoreUtils.ComputeRelativePath(record.GetFileName(Database), context.FileName)));
            if (context.Options.ShouldWriteTypeNames)
                fileReferenceNode.Add(new XAttribute("__typeName", Database.StructDefinitions[record.StructIndex].GetName(Database)));
            return fileReferenceNode;
        }

        return GetFromInstance(name, record.StructIndex, record.InstanceIndex, context, overrideName).WithAttribute("recordGuid", record.Id.ToString());
    }

    public XElement GetFromMainRecord(DataCoreRecord record, DataCoreExtractionContext<XElement> context)
    {
        if (!Database.MainRecords.Contains(record.Id))
            throw new InvalidOperationException("Can only extract main records");

        var recordName = record.GetName(Database)
            .Replace("'", "_")
            .Replace(" ", "_")
            .Replace("/", "_")
            .Replace("&", "_");

        var element = GetFromInstance(recordName, record.StructIndex, record.InstanceIndex, context)
            .WithAttribute("__recordGuid", record.Id.ToString());

        //add weak pointers ids, so we can actually see what a weak pointer is pointing at
        foreach (var weakPtr in context.GetWeakPointers())
        {
            var pointedAtElement = context.Elements[(weakPtr.structIndex, weakPtr.instanceIndex)];

            pointedAtElement.Add(new XAttribute("__weakPointerId", context.GetWeakPointerId(weakPtr.structIndex, weakPtr.instanceIndex).ToString(CultureInfo.InvariantCulture)));
        }

        return element;
    }

    private XElement GetFromPointer(string name, DataCorePointer pointer, DataCoreExtractionContext<XElement> context, bool overrideName = false) =>
        GetFromInstance(name, pointer.StructIndex, pointer.InstanceIndex, context, overrideName);

    private XElement GetFromInstance(string name, int structIndex, int instanceIndex, DataCoreExtractionContext<XElement> context, bool overrideName = false)
    {
        if (structIndex == -1 || instanceIndex == -1)
            return GetNull(name, context, structIndex, instanceIndex);

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

        WriteMetadata(element, structIndex, instanceIndex, context);
        WriteTypeNames(element, structIndex, context);

        return element;
    }

    private XElement GetWeakPointer(string name, DataCorePointer pointer, DataCoreExtractionContext<XElement> context, bool overrideName = false)
    {
        if (pointer.InstanceIndex == -1 || pointer.StructIndex == -1)
            return GetNull(name, context, pointer.StructIndex, pointer.InstanceIndex);

        var weakPointer = new XElement(Database.StructDefinitions[pointer.StructIndex].GetName(Database));

        var pointerId = context.AddWeakPointer(pointer.StructIndex, pointer.InstanceIndex);

        weakPointer.Add(new XAttribute("__weakPointerId", pointerId.ToString(CultureInfo.InvariantCulture)));
        weakPointer.Add(new XAttribute("__pointsTo_Name", name));
        weakPointer.Add(new XAttribute("__pointsTo_TypeName", Database.StructDefinitions[pointer.StructIndex].GetName(Database)));

        WriteMetadata(weakPointer, pointer.StructIndex, pointer.InstanceIndex, context);
        WriteTypeNames(weakPointer, pointer.StructIndex, context);

        return weakPointer;
    }

    private XElement GetNull(string name, DataCoreExtractionContext<XElement> context, int structIndex, int instanceIndex)
    {
        var element = new XElement(name);
        element.Add(new XAttribute("__value", "null"));

        WriteMetadata(element, structIndex, instanceIndex, context);
        WriteTypeNames(element, structIndex, context);

        return element;
    }

    private void WriteTypeNames(XElement element, int structIndex, DataCoreExtractionContext<XElement> context)
    {
        if (structIndex == -1)
            return;

        var structType = Database.StructDefinitions[structIndex];

        if (context.Options.ShouldWriteTypeNames)
            element.Add(new XAttribute("__typeName", structType.GetName(Database)));

        if (context.Options.ShouldWriteBaseTypeNames)
        {
            var i = 0;
            var @this = structType;
            while (@this.ParentTypeIndex != -1)
            {
                @this = Database.StructDefinitions[@this.ParentTypeIndex];
                element.Add(new XAttribute($"__baseTypeName{i}", @this.GetName(Database)));
                i++;
            }
        }
    }

    private static void WriteMetadata(XElement element, int structIndex, int instanceIndex, DataCoreExtractionContext<XElement> context)
    {
        if (!context.Options.ShouldWriteMetadata)
            return;

        element.Add(new XAttribute("__structIndex", structIndex.ToString(CultureInfo.InvariantCulture)));
        element.Add(new XAttribute("__instanceIndex", instanceIndex.ToString(CultureInfo.InvariantCulture)));
    }
}