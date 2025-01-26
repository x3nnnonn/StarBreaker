using StarBreaker.Common;
using StarBreaker.DataCore;

namespace StarBreaker.DataCoreGenerated;

internal static class DataCoreHelper
{
    public static T? ReadFromReference<T>(DataCoreDatabase db, DataCoreReference reference) where T : class, IDataCoreReadable<T>
    {
        if (reference.RecordId == CigGuid.Empty || reference.InstanceIndex == -1)
            return null;

        if (db.MainRecords.TryGetValue(reference.RecordId, out var mr))
            return null;

        var record = db.GetRecord(reference.RecordId);
        return ReadFromInstance<T>(db, record.StructIndex, record.InstanceIndex);
    }

    public static T? ReadFromPointer<T>(DataCoreDatabase db, DataCorePointer pointer) where T : class, IDataCoreReadable<T>
    {
        return ReadFromInstance<T>(db, pointer.StructIndex, pointer.InstanceIndex);
    }
    
    public static T? ReadFromInstance<T>(DataCoreDatabase db, int structIndex, int instanceIndex) where T : class, IDataCoreReadable<T>
    {
        if (structIndex == -1 || instanceIndex == -1)
            return null;

        var reader = db.GetReader(structIndex, instanceIndex);
        return T.Read(db, ref reader);
    }

    public static T EnumParse<T>(string value, T unknown) where T : struct, Enum
    {
        if (value == "")
            return unknown;

        if (!Enum.TryParse<T>(value, out var eVal))
        {
            var type = typeof(T);
            Console.WriteLine($"Error parsing Enum of type {type.Name} with value {value}. Setting to unknown.");
            return unknown;
        }
        return eVal;
    }

    public static T?[] ReadReferenceArray<T>(DataCoreDatabase db, ref SpanReader reader) where T : class, IDataCoreReadable<T>
    {
        var count = reader.ReadInt32();
        var firstIndex = reader.ReadInt32();

        var array = new T?[count];

        for (var i = firstIndex; i < firstIndex + count; i++)
        {
            array[i - firstIndex] = ReadFromReference<T>(db, db.ReferenceValues[i]);
        }

        return array;
    }

    public static T[] ReadWeakPointerArray<T>(DataCoreDatabase db, ref SpanReader reader)
        //where T  : IDataCoreReadable<T>
    {
        var count = reader.ReadInt32();
        var firstIndex = reader.ReadInt32();

        var array = new T[count];

        for (var i = firstIndex; i < firstIndex + count; i++)
        {
            //TODO: causes recursive loop
            //array[i - firstIndex] = ReadFromPointer<T>(db, db.WeakValues[i]);
            array[i - firstIndex] = default;
        }

        return array;
    }

    public static Lazy<T>[] ReadWeakPointerArrayLazy<T>(DataCoreDatabase db, ref SpanReader reader) where T : class, IDataCoreReadable<T>
    {
        var count = reader.ReadInt32();
        var firstIndex = reader.ReadInt32();

        var array = new Lazy<T>[count];

        for (var i = firstIndex; i < firstIndex + count; i++)
        {
            var ptr = db.WeakValues[i];
            array[i - firstIndex] = new Lazy<T>(() => ReadFromPointer<T>(db, ptr));
        }

        return array;
    }

    public static T?[] ReadStrongPointerArray<T>(DataCoreDatabase db, ref SpanReader reader) where T : class, IDataCoreReadable<T>
    {
        var count = reader.ReadInt32();
        var firstIndex = reader.ReadInt32();

        var array = new T?[count];

        for (var i = firstIndex; i < firstIndex + count; i++)
        {
            var strongValue = db.StrongValues[i];
            var read = TypeMap.ReadFromRecord(db, strongValue.StructIndex, strongValue.InstanceIndex);
            if (read == null)
                array[i - firstIndex] = null!;
            else if (read is T readable)
                array[i - firstIndex] = readable;
            else 
                throw new Exception($"ReadFromPointer failed to cast {read.GetType()} to {typeof(T)}");
        }

        return array;
    }

    public static T[] ReadClassArray<T>(DataCoreDatabase db, ref SpanReader reader, int structIndex) where T : class, IDataCoreReadable<T>
    {
        var count = reader.ReadInt32();
        var firstIndex = reader.ReadInt32();

        var array = new T[count];

        for (var i = firstIndex; i < firstIndex + count; i++)
        {
            array[i - firstIndex] = ReadFromInstance<T>(db, structIndex, i) 
                                    ?? throw new Exception($"ReadFromInstance failed to read instance of {typeof(T)}");
        }

        return array;
    }

    public static T[] ReadEnumArray<T>(DataCoreDatabase db, ref SpanReader reader) where T : struct, Enum
    {
        var count = reader.ReadInt32();
        var firstIndex = reader.ReadInt32();

        var array = new T[count];

        for (var i = firstIndex; i < firstIndex + count; i++)
        {
            array[i - firstIndex] = EnumParse<T>(db.EnumValues[i].ToString(db), default);
        }

        return array;
    }

    public static string[] ReadStringArray(DataCoreDatabase db, ref SpanReader reader)
    {
        var count = reader.ReadInt32();
        var firstIndex = reader.ReadInt32();
        var result = new string[count];
        
        for (var i = 0; i < count; i++)
            result[i] = db.StringIdValues[firstIndex + i].ToString(db);
        
        return result;
    }

    public static string[] ReadLocaleArray(DataCoreDatabase db, ref SpanReader reader)
    {
        var count = reader.ReadInt32();
        var firstIndex = reader.ReadInt32();
        var result = new string[count];
        
        for (var i = 0; i < count; i++)
            result[i] = db.LocaleValues[firstIndex + i].ToString(db);
        
        return result;
    }

    public static sbyte[] ReadSByteArray(DataCoreDatabase db, ref SpanReader reader)
    {
        var count = reader.ReadInt32();
        var firstIndex = reader.ReadInt32();
        return db.Int8Values.AsSpan(firstIndex, count).ToArray();
    }

    public static short[] ReadInt16Array(DataCoreDatabase db, ref SpanReader reader)
    {
        var count = reader.ReadInt32();
        var firstIndex = reader.ReadInt32();
        return db.Int16Values.AsSpan(firstIndex, count).ToArray();
    }

    public static int[] ReadInt32Array(DataCoreDatabase db, ref SpanReader reader)
    {
        var count = reader.ReadInt32();
        var firstIndex = reader.ReadInt32();
        return db.Int32Values.AsSpan(firstIndex, count).ToArray();
    }

    public static long[] ReadInt64Array(DataCoreDatabase db, ref SpanReader reader)
    {
        var count = reader.ReadInt32();
        var firstIndex = reader.ReadInt32();
        return db.Int64Values.AsSpan(firstIndex, count).ToArray();
    }

    public static byte[] ReadByteArray(DataCoreDatabase db, ref SpanReader reader)
    {
        var count = reader.ReadInt32();
        var firstIndex = reader.ReadInt32();
        return db.UInt8Values.AsSpan(firstIndex, count).ToArray();
    }

    public static ushort[] ReadUInt16Array(DataCoreDatabase db, ref SpanReader reader)
    {
        var count = reader.ReadInt32();
        var firstIndex = reader.ReadInt32();
        return db.UInt16Values.AsSpan(firstIndex, count).ToArray();
    }

    public static uint[] ReadUInt32Array(DataCoreDatabase db, ref SpanReader reader)
    {
        var count = reader.ReadInt32();
        var firstIndex = reader.ReadInt32();
        return db.UInt32Values.AsSpan(firstIndex, count).ToArray();
    }

    public static ulong[] ReadUInt64Array(DataCoreDatabase db, ref SpanReader reader)
    {
        var count = reader.ReadInt32();
        var firstIndex = reader.ReadInt32();
        return db.UInt64Values.AsSpan(firstIndex, count).ToArray();
    }

    public static bool[] ReadBoolArray(DataCoreDatabase db, ref SpanReader reader)
    {
        var count = reader.ReadInt32();
        var firstIndex = reader.ReadInt32();
        return db.BooleanValues.AsSpan(firstIndex, count).ToArray();
    }

    public static float[] ReadSingleArray(DataCoreDatabase db, ref SpanReader reader)
    {
        var count = reader.ReadInt32();
        var firstIndex = reader.ReadInt32();
        return db.SingleValues.AsSpan(firstIndex, count).ToArray();
    }

    public static double[] ReadDoubleArray(DataCoreDatabase db, ref SpanReader reader)
    {
        var count = reader.ReadInt32();
        var firstIndex = reader.ReadInt32();
        return db.DoubleValues.AsSpan(firstIndex, count).ToArray();
    }
}