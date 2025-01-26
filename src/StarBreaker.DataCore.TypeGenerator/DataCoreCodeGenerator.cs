using System.Text;

namespace StarBreaker.DataCore.TypeGenerator;

public class DataCoreTypeGenerator
{
    private readonly DataCoreDatabase Database;

    public DataCoreTypeGenerator(DataCoreDatabase database)
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
                sb.AppendLine($"public record {structDefinition.GetName(Database)} : IDataCoreReadable<{structDefinition.GetName(Database)}>");
            }

            sb.AppendLine("{");
            var properties = Database.PropertyDefinitions.AsSpan(structDefinition.FirstAttributeIndex, structDefinition.AttributeCount);

            foreach (var property in properties)
            {
                var propertyType = GetPropertyType(property);
                var name = property.GetName(Database);

                sb.AppendLine($"    public required {propertyType} @{name} {{ get; init; }}");
            }

            sb.AppendLine();

            WriteSpecialConstructor(sb, structDefinition, structIndex);

            sb.AppendLine("}");

            var final = sb.ToString();

            File.WriteAllText(Path.Combine(path, "Types", fileName), final);
        }
    }

    private string GetScalarPropertyType(DataCorePropertyDefinition property) => property.DataType switch
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

        DataType.EnumChoice => Database.EnumDefinitions[property.StructIndex].GetName(Database),
        DataType.Reference => $"{Database.StructDefinitions[property.StructIndex].GetName(Database)}?",
        DataType.StrongPointer => $"{Database.StructDefinitions[property.StructIndex].GetName(Database)}?",
        DataType.Class => Database.StructDefinitions[property.StructIndex].GetName(Database),

        DataType.WeakPointer => "DataCorePointer",
        // DataType.WeakPointer => Database.StructDefinitions[property.StructIndex].GetName(Database)?,
        _ => throw new ArgumentOutOfRangeException()
    };
    
    private string GetGenericPropertyType(DataCorePropertyDefinition property) => property.DataType switch
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

        DataType.EnumChoice => Database.EnumDefinitions[property.StructIndex].GetName(Database),
        DataType.Reference => Database.StructDefinitions[property.StructIndex].GetName(Database),
        DataType.StrongPointer => Database.StructDefinitions[property.StructIndex].GetName(Database),
        DataType.Class => Database.StructDefinitions[property.StructIndex].GetName(Database),

        DataType.WeakPointer => "DataCorePointer",
        // DataType.WeakPointer => Database.StructDefinitions[property.StructIndex].GetName(Database)?,
        _ => throw new ArgumentOutOfRangeException()
    };

    private string GetArrayPropertyType(DataCorePropertyDefinition property) => property.DataType switch
    {
        DataType.Boolean => "bool[]",
        DataType.Byte => "byte[]",
        DataType.SByte => "sbyte[]",
        DataType.Int16 => "short[]",
        DataType.UInt16 => "ushort[]",
        DataType.Int32 => "int[]",
        DataType.UInt32 => "uint[]",
        DataType.Int64 => "long[]",
        DataType.UInt64 => "ulong[]",
        DataType.Single => "float[]",
        DataType.Double => "double[]",
        DataType.Guid => "CigGuid[]",
        DataType.Locale => "string[]",
        DataType.String => "string[]",

        DataType.EnumChoice => $"{Database.EnumDefinitions[property.StructIndex].GetName(Database)}[]",
        DataType.Reference => $"{Database.StructDefinitions[property.StructIndex].GetName(Database)}?[]",
        DataType.StrongPointer => $"{Database.StructDefinitions[property.StructIndex].GetName(Database)}?[]",
        DataType.Class => $"{Database.StructDefinitions[property.StructIndex].GetName(Database)}[]",

        DataType.WeakPointer => "DataCorePointer[]",
        // DataType.WeakPointer => $"{Database.StructDefinitions[property.StructIndex].GetName(Database)}?[]",
        _ => throw new ArgumentOutOfRangeException()
    };

    private string GetPropertyType(DataCorePropertyDefinition property) => property.ConversionType switch
    {
        ConversionType.Attribute => GetScalarPropertyType(property),
        _ => GetArrayPropertyType(property)
    };

    private void WriteSpecialConstructor(StringBuilder sb, DataCoreStructDefinition structDefinition, int structIndex)
    {
        if (structDefinition.ParentTypeIndex != -1)
            sb.AppendLine($"    public new static {structDefinition.GetName(Database)} Read(DataCoreDatabase db, ref SpanReader reader)");
        else
            sb.AppendLine($"    public static {structDefinition.GetName(Database)} Read(DataCoreDatabase db, ref SpanReader reader)");

        var allprops = Database.GetProperties(structIndex).AsSpan();

        //for now we ignore parent types
        sb.AppendLine("    {");

        foreach (var property in allprops)
        {
            if (property.ConversionType == ConversionType.Attribute)
                WriteSingleRead(sb, property);
            else
                WriteArrayRead(sb, property);
        }

        sb.AppendLine();
        sb.AppendLine($"        return new {structDefinition.GetName(Database)}");
        sb.AppendLine("        {");

        foreach (var property in allprops)
        {
            var name = property.GetName(Database);
            sb.AppendLine($"            @{name} = _{name},");
        }


        sb.AppendLine("        };");

        sb.AppendLine("    }");
    }

    private void WriteSingleRead(StringBuilder sb, DataCorePropertyDefinition property)
    {
        var propertyType = GetGenericPropertyType(property);
        var name = property.GetName(Database);

        switch (property.DataType)
        {
            case DataType.Class:
                sb.AppendLine($"        var _{name} = {propertyType}.Read(db, ref reader);");
                break;
            case DataType.EnumChoice:
                var enumName = Database.EnumDefinitions[property.StructIndex].GetName(Database);
                sb.AppendLine($"        var _{name} = DataCoreHelper.EnumParse(reader.Read<DataCoreStringId>().ToString(db), {enumName}.__Unknown);");
                break;
            case DataType.Reference:
                sb.AppendLine($"        var _{name} = DataCoreHelper.ReadFromReference<{propertyType}>(db, reader.Read<DataCoreReference>());");
                break;
            case DataType.StrongPointer:
                sb.AppendLine($"        var _{name} = DataCoreHelper.ReadFromPointer<{propertyType}>(db, reader.Read<DataCorePointer>());");
                break;
            case DataType.WeakPointer:
                //do as default. we probably should handle this, it's actually feasible now :D
                sb.AppendLine($"        var _{name} = reader.Read<{propertyType}>();");
                break;
            case DataType.String:
            case DataType.Locale:
                sb.AppendLine($"        var _{name} = reader.Read<DataCoreStringId>().ToString(db);");
                break;
            case DataType.Guid:
            case DataType.Double:
            case DataType.Single:
            case DataType.UInt64:
            case DataType.UInt32:
            case DataType.UInt16:
            case DataType.Byte:
            case DataType.Int64:
            case DataType.Int32:
            case DataType.Int16:
            case DataType.SByte:
            case DataType.Boolean:
                //this one should be fine for everything else.
                sb.AppendLine($"        var _{name} = reader.Read<{propertyType}>();");
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void WriteArrayRead(StringBuilder sb, DataCorePropertyDefinition property)
    {
        var propertyType = GetGenericPropertyType(property);
        var name = property.GetName(Database);

        switch (property.DataType)
        {
            case DataType.Reference:
                sb.AppendLine($"        var _{name} = DataCoreHelper.ReadReferenceArray<{propertyType}>(db, ref reader);");
                break;
            case DataType.StrongPointer:
                sb.AppendLine($"        var _{name} = DataCoreHelper.ReadStrongPointerArray<{propertyType}>(db, ref reader);");
                break;
            case DataType.WeakPointer:
                sb.AppendLine($"        var _{name} = DataCoreHelper.ReadWeakPointerArray<{propertyType}>(db, ref reader);");
                break;
            case DataType.Class:
                sb.AppendLine($"        var _{name} = DataCoreHelper.ReadClassArray<{propertyType}>(db, ref reader, {property.StructIndex});");
                break;
            case DataType.Boolean:
                sb.AppendLine($"        var _{name} = DataCoreHelper.ReadBoolArray(db, ref reader);");
                break;
            case DataType.Byte:
                sb.AppendLine($"        var _{name} = DataCoreHelper.ReadByteArray(db, ref reader);");
                break;
            case DataType.SByte:
                sb.AppendLine($"        var _{name} = DataCoreHelper.ReadSByteArray(db, ref reader);");
                break;
            case DataType.Int16:
                sb.AppendLine($"        var _{name} = DataCoreHelper.ReadInt16Array(db, ref reader);");
                break;
            case DataType.UInt16:
                sb.AppendLine($"        var _{name} = DataCoreHelper.ReadUInt16Array(db, ref reader);");
                break;
            case DataType.Int32:
                sb.AppendLine($"        var _{name} = DataCoreHelper.ReadInt32Array(db, ref reader);");
                break;
            case DataType.UInt32:
                sb.AppendLine($"        var _{name} = DataCoreHelper.ReadUInt32Array(db, ref reader);");
                break;
            case DataType.Int64:
                sb.AppendLine($"        var _{name} = DataCoreHelper.ReadInt64Array(db, ref reader);");
                break;
            case DataType.UInt64:
                sb.AppendLine($"        var _{name} = DataCoreHelper.ReadUInt64Array(db, ref reader);");
                break;
            case DataType.Single:
                sb.AppendLine($"        var _{name} = DataCoreHelper.ReadSingleArray(db, ref reader);");
                break;
            case DataType.Double:
                sb.AppendLine($"        var _{name} = DataCoreHelper.ReadDoubleArray(db, ref reader);");
                break;
            case DataType.Guid:
                sb.AppendLine($"        var _{name} = DataCoreHelper.ReadGuidArray(db, ref reader);");
                break;
            case DataType.Locale:
                sb.AppendLine($"        var _{name} = DataCoreHelper.ReadLocaleArray(db, ref reader);");
                break;
            case DataType.String:
                sb.AppendLine($"        var _{name} = DataCoreHelper.ReadStringArray(db, ref reader);");
                break;
            case DataType.EnumChoice:
                sb.AppendLine($"        var _{name} = DataCoreHelper.ReadEnumArray<{Database.EnumDefinitions[property.StructIndex].GetName(Database)}>(db, ref reader);");
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}