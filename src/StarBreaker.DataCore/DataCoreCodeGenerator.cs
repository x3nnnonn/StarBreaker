using System.Text;

namespace StarBreaker.DataCore;

public class DataCoreCodeGenerator
{
    private readonly DataCoreDatabase Database;

    public DataCoreCodeGenerator(DataCoreDatabase database)
    {
        Database = database;
    }

    //Note: This is not a standard source generator because those don't really deal well with the way
    // we are getting the type information. We're reading a pretty big 200mb blob with random data in there,
    // usually standard source generators use the code itself or sometimes small and simple text files.
    public void Generate(string path)
    {
        Directory.CreateDirectory(path);

        GenerateTypes(path);
        GenerateEnums(path);
        GenerateTypeMap(path);
    }

    private void GenerateTypeMap(string path)
    {
        Directory.CreateDirectory(Path.Combine(path));

        var typeMapSb = new StringBuilder();
        //map the struct indexes to the generated types
        typeMapSb.AppendLine("using System;");
        typeMapSb.AppendLine("using System.Collections.Generic;");
        typeMapSb.AppendLine("using StarBreaker.DataCore;");
        typeMapSb.AppendLine();
        typeMapSb.AppendLine("namespace StarBreaker.DataCoreGenerated;");
        typeMapSb.AppendLine();
        typeMapSb.AppendLine("public static class TypeMap");
        typeMapSb.AppendLine("{");
        typeMapSb.AppendLine("    public static IDataCoreReadable? ReadFromRecord(DataCoreDatabase db, int structIndex, int instanceIndex)");
        typeMapSb.AppendLine("    {");
        typeMapSb.AppendLine("        if (structIndex == -1 || instanceIndex == -1)");
        typeMapSb.AppendLine("            return null;");
        typeMapSb.AppendLine();
        typeMapSb.AppendLine("        return structIndex switch");
        typeMapSb.AppendLine("        {");
        for (var i = 0; i < Database.StructDefinitions.Length; i++)
        {
            var structDefinition = Database.StructDefinitions[i];
            typeMapSb.AppendLine($"            {i} => DataCoreHelper.ReadFromInstance<{structDefinition.GetName(Database)}>(db, structIndex, instanceIndex),");
        }

        typeMapSb.AppendLine("            _ => throw new NotImplementedException()");
        typeMapSb.AppendLine("        };");

        typeMapSb.AppendLine("    }");
        typeMapSb.AppendLine("}");

        File.WriteAllText(Path.Combine(path, "TypeMap.cs"), typeMapSb.ToString());
    }

    private void GenerateEnums(string path)
    {
        Directory.CreateDirectory(Path.Combine(path, "Enums"));
        foreach (var enumDefinition in Database.EnumDefinitions)
        {
            var fileName = enumDefinition.GetName(Database) + ".cs";
            var sb = new StringBuilder();

            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Text.Json.Serialization;");
            sb.AppendLine("using StarBreaker.DataCore;");

            sb.AppendLine();
            sb.AppendLine("namespace StarBreaker.DataCoreGenerated;");
            sb.AppendLine();

            sb.AppendLine($"public enum {enumDefinition.GetName(Database)} : int");
            sb.AppendLine("{");

            if ("SinglePlayerOrMultiplayer" == enumDefinition.GetName(Database))
            {
                Console.WriteLine();
            }

            sb.AppendLine($"    __Unknown = -1,");

            for (var i = 0; i < enumDefinition.ValueCount; i++)
            {
                sb.AppendLine($"    {Database.EnumOptions[enumDefinition.FirstValueIndex + i].ToString(Database)},");
            }

            sb.AppendLine("}");

            var final = sb.ToString();

            File.WriteAllText(Path.Combine(path, "Enums", fileName), final);
        }
    }

    private void GenerateTypes(string path)
    {
        Directory.CreateDirectory(Path.Combine(path, "Types"));
        for (var structIndex = 0; structIndex < Database.StructDefinitions.Length; structIndex++)
        {
            var structDefinition = Database.StructDefinitions[structIndex];
            //write each struct definition to a file
            var fileName = structDefinition.GetName(Database) + ".cs";
            var sb = new StringBuilder();

            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Text.Json.Serialization;");
            sb.AppendLine("using StarBreaker.DataCore;");
            sb.AppendLine("using StarBreaker.Common;");
            sb.AppendLine();
            sb.AppendLine("namespace StarBreaker.DataCoreGenerated;");
            sb.AppendLine();

            if (structDefinition.ParentTypeIndex != -1)
            {
                var parent = Database.StructDefinitions[structDefinition.ParentTypeIndex];
                sb.AppendLine($"public record {structDefinition.GetName(Database)} : {parent.GetName(Database)}, IDataCoreReadable<{structDefinition.GetName(Database)}>");
            }
            else
            {
                //sb.AppendLine($"public class {structDefinition.GetName(Database)}");
                sb.AppendLine($"public record {structDefinition.GetName(Database)} : IDataCoreReadable<{structDefinition.GetName(Database)}>");
            }

            sb.AppendLine("{");
            var properties = Database.PropertyDefinitions.AsSpan(structDefinition.FirstAttributeIndex, structDefinition.AttributeCount);

            foreach (var property in properties)
            {
                var propertyType = GetPropertyType(property);
                var name = property.GetName(Database);

                sb.AppendLine($"    public {propertyType} @{name} {{ get; init; }}");
            }

            sb.AppendLine();

            WriteSpecialConstructor(sb, structDefinition, structIndex);

            sb.AppendLine("}");

            var final = sb.ToString();

            File.WriteAllText(Path.Combine(path, "Types", fileName), final);
        }
    }

    private void WriteBasicConstructor(StringBuilder sb, DataCoreStructDefinition structDefinition, int structIndex)
    {
        // The constructor should take as arguments the properties in the order we expect.
        // Which is base type -> derived type -> derived type -> our type strictly.
        // then we handle passing the properties to the following constructor.
        var allprops = Database.GetProperties(structIndex).AsSpan();
        sb.AppendLine($"    public {structDefinition.GetName(Database)}(");
        for (var i = 0; i < allprops.Length; i++)
        {
            var property = allprops[i];
            var propertyType = GetPropertyType(property);
            var name = property.GetName(Database);

            sb.Append($"        {propertyType} _{name}");
            if (i != allprops.Length - 1)
                sb.AppendLine(",");
            else sb.AppendLine();
        }

        sb.AppendLine("    )");

        if (structDefinition.ParentTypeIndex != -1)
        {
            sb.AppendLine("        : base(");
            //we take all properties from all parent types. We will consume our own in our constructor, and pass down the rest.
            var propsForConstructor = allprops[..^structDefinition.AttributeCount];
            for (var i = 0; i < propsForConstructor.Length; i++)
            {
                var property = propsForConstructor[i];
                var name = property.GetName(Database);
                sb.Append($"            _{name}");
                if (i != propsForConstructor.Length - 1)
                    sb.AppendLine(",");
                else sb.AppendLine();
            }

            sb.AppendLine("        )");
        }

        sb.AppendLine("    {");
        var thisProps = Database.PropertyDefinitions.AsSpan(structDefinition.FirstAttributeIndex, structDefinition.AttributeCount);
        foreach (var property in thisProps)
        {
            var name = property.GetName(Database);
            sb.AppendLine($"        @{name} = _{name};");
        }

        sb.AppendLine("    }");
    }

    private string GetSimplePropertyType(DataCorePropertyDefinition property) => property.DataType switch
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
        DataType.Locale => "DataCoreStringId",
        DataType.String => "DataCoreStringId",

        DataType.EnumChoice => Database.EnumDefinitions[property.StructIndex].GetName(Database),
        DataType.Reference => Database.StructDefinitions[property.StructIndex].GetName(Database),
        DataType.WeakPointer => "DataCorePointer",
        DataType.StrongPointer => Database.StructDefinitions[property.StructIndex].GetName(Database),
        DataType.Class => Database.StructDefinitions[property.StructIndex].GetName(Database),

        //todo
        // DataType.EnumChoice => Database.EnumDefinitions[property.StructIndex].GetName(Database),
        // DataType.Class => Database.StructDefinitions[property.StructIndex].GetName(Database),
        // DataType.Reference => Database.StructDefinitions[property.StructIndex].GetName(Database),
        // DataType.WeakPointer => Database.StructDefinitions[property.StructIndex].GetName(Database),
        // DataType.StrongPointer => Database.StructDefinitions[property.StructIndex].GetName(Database),
        _ => throw new ArgumentOutOfRangeException()
    };


    private string GetPropertyType(DataCorePropertyDefinition property)
    {
        var baseProperty = GetSimplePropertyType(property);

        return property.ConversionType switch
        {
            ConversionType.Attribute => baseProperty,
            _ => $"{baseProperty}[]"
        };
    }

    //TODO: generate a constructor that accepts something useful like a SpanReader and a database.
    // it should then, based on its properties, generate  either:
    // if attribute, reader.Read<t>(). For struct types, we *have* to pass down the spanreader or it gets out of sync.
    //      For references or pointers, we can just read that and ski the actual data for a POC impl.
    // if array, we probably pass it down to a generic method that handles reading the array i and count, and reads the elements as needed.
    //      for a poc, realistically we read those two ints and skip. things should work fine from there on even half complete.

    private void WriteSpecialConstructor(StringBuilder sb, DataCoreStructDefinition structDefinition, int structIndex)
    {
        if (structDefinition.ParentTypeIndex != -1)
            sb.AppendLine($"    public new static {structDefinition.GetName(Database)} Read(DataCoreDatabase db, DataCoreStructDefinition structDefinition, ref SpanReader reader)");
        else
            sb.AppendLine($"    public static {structDefinition.GetName(Database)} Read(DataCoreDatabase db, DataCoreStructDefinition structDefinition, ref SpanReader reader)");

        var allprops = Database.GetProperties(structIndex).AsSpan();

        //for now we ignore parent types
        sb.AppendLine("    {");
        sb.AppendLine($"        return new {structDefinition.GetName(Database)}");
        sb.AppendLine("        {");

        foreach (var property in allprops)
        {
            if (property.ConversionType == ConversionType.Attribute)
                WriteSingleRead(sb, property);
            else
                WriteArrayRead(sb, property);
        }

        sb.AppendLine("        };");

        sb.AppendLine("    }");
    }

    private void WriteSingleRead(StringBuilder sb, DataCorePropertyDefinition property)
    {
        var propertyType = GetSimplePropertyType(property);
        var name = property.GetName(Database);

        switch (property.DataType)
        {
            case DataType.Class:
                sb.AppendLine($"            @{name} = {propertyType}.Read(db, structDefinition, ref reader),");
                break;
            case DataType.EnumChoice:
            {
                var enumName = Database.EnumDefinitions[property.StructIndex].GetName(Database);
                sb.AppendLine($"            @{name} = DataCoreHelper.EnumParse<{enumName}>(reader.Read<DataCoreStringId>().ToString(db), {enumName}.__Unknown),");
                break;
            }
            case DataType.Reference:
                sb.AppendLine($"            @{name} = DataCoreHelper.ReadFromReference<{propertyType}>(db, reader.Read<DataCoreReference>()),");
                break;
            case DataType.StrongPointer:
                sb.AppendLine($"            @{name} = DataCoreHelper.ReadFromPointer<{propertyType}>(db, reader.Read<DataCorePointer>()),");
                break;
            case DataType.WeakPointer:
                //do as default. we probably should handle this, it's actually feasible now :D
                sb.AppendLine($"            @{name} = reader.Read<{propertyType}>(),");
                break;
            default:
                //this one should be fine for everything else.
                sb.AppendLine($"            @{name} = reader.Read<{propertyType}>(),");
                break;
        }
    }

    private void WriteArrayRead(StringBuilder sb, DataCorePropertyDefinition property)
    {
        var propertyType = GetSimplePropertyType(property);
        var name = property.GetName(Database);

        switch (property.DataType)
        {
            case DataType.Reference:
                sb.AppendLine($"            @{name} = DataCoreHelper.ReadReferenceArray<{propertyType}>(db, ref reader),");
                break;
            case DataType.StrongPointer:
                sb.AppendLine($"            @{name} = DataCoreHelper.ReadStrongPointerArray<{propertyType}>(db, ref reader),");
                break;
            case DataType.WeakPointer:
                sb.AppendLine($"            @{name} = DataCoreHelper.ReadWeakPointerArray<{propertyType}>(db, ref reader),");
                break;
            case DataType.Class:
                sb.AppendLine($"            @{name} = DataCoreHelper.ReadClassArray<{propertyType}>(db, ref reader, {property.StructIndex}),");
                break;
            case DataType.Boolean:
                sb.AppendLine($"            @{name} = DataCoreHelper.ReadBoolArray(db, ref reader),");
                break;
            case DataType.Byte:
                sb.AppendLine($"            @{name} = DataCoreHelper.ReadByteArray(db, ref reader),");
                break;
            case DataType.SByte:
                sb.AppendLine($"            @{name} = DataCoreHelper.ReadSByteArray(db, ref reader),");
                break;
            case DataType.Int16:
                sb.AppendLine($"            @{name} = DataCoreHelper.ReadInt16Array(db, ref reader),");
                break;
            case DataType.UInt16:
                sb.AppendLine($"            @{name} = DataCoreHelper.ReadUInt16Array(db, ref reader),");
                break;
            case DataType.Int32:
                sb.AppendLine($"            @{name} = DataCoreHelper.ReadInt32Array(db, ref reader),");
                break;
            case DataType.UInt32:
                sb.AppendLine($"            @{name} = DataCoreHelper.ReadUInt32Array(db, ref reader),");
                break;
            case DataType.Int64:
                sb.AppendLine($"            @{name} = DataCoreHelper.ReadInt64Array(db, ref reader),");
                break;
            case DataType.UInt64:
                sb.AppendLine($"            @{name} = DataCoreHelper.ReadUInt64Array(db, ref reader),");
                break;
            case DataType.Single:
                sb.AppendLine($"            @{name} = DataCoreHelper.ReadSingleArray(db, ref reader),");
                break;
            case DataType.Double:
                sb.AppendLine($"            @{name} = DataCoreHelper.ReadDoubleArray(db, ref reader),");
                break;
            case DataType.Guid:
                sb.AppendLine($"            @{name} = DataCoreHelper.ReadGuidArray(db, ref reader),");
                break;
            case DataType.Locale:
                sb.AppendLine($"            @{name} = DataCoreHelper.ReadLocaleArray(db, ref reader),");
                break;
            case DataType.String:
                sb.AppendLine($"            @{name} = DataCoreHelper.ReadStringArray(db, ref reader),");
                break;
            case DataType.EnumChoice:
            {
                var enumName = Database.EnumDefinitions[property.StructIndex].GetName(Database);
                sb.AppendLine($"            @{name} = DataCoreHelper.ReadEnumArray<{enumName}>(db, ref reader),");
                break;
            }
            default:
                sb.AppendLine($"            @{name} = DataCoreHelper.ReadDummyArray<{propertyType}>(db, ref reader),");
                break;
        }
    }
}