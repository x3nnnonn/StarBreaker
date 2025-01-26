using StarBreaker.Common;
using StarBreaker.DataCore;

namespace StarBreaker.DataCoreGenerated;

//TODO: come up with a way of hashing the types we generated, then verify that the datacore file we're reading matches the types we have.
// if they don't match we *WILL* fail to read it. We should throw before this.
public sealed partial class DataCoreBinaryGenerated : IDataCoreBinary<IDataCoreReadable>
{
    public DataCoreDatabase Database { get; }

    public DataCoreBinaryGenerated(DataCoreDatabase database)
    {
        Database = database;
    }

    public IDataCoreReadable GetFromMainRecord(DataCoreRecord record)
    {
        var data = ReadFromRecord(record.StructIndex, record.InstanceIndex);

        if (data == null)
            throw new InvalidOperationException($"Failed to read data from record {record}");

        return data;
    }

    public void SaveToFile(DataCoreRecord record, string path)
    {
        throw new NotImplementedException();
    }

    public T? ReadFromReference<T>(DataCoreReference reference) where T : class, IDataCoreReadable<T>
    {
        if (reference.RecordId == CigGuid.Empty || reference.InstanceIndex == -1)
            return null;

        if (Database.MainRecords.TryGetValue(reference.RecordId, out var mr))
            return null;

        var record = Database.GetRecord(reference.RecordId);
        return ReadFromInstance<T>(record.StructIndex, record.InstanceIndex);
    }

    public T? ReadFromPointer<T>(DataCorePointer pointer) where T : class, IDataCoreReadable<T>
    {
        return ReadFromInstance<T>(pointer.StructIndex, pointer.InstanceIndex);
    }

    public T? ReadFromInstance<T>(int structIndex, int instanceIndex) where T : class, IDataCoreReadable<T>
    {
        if (structIndex == -1 || instanceIndex == -1)
            return null;

        var reader = Database.GetReader(structIndex, instanceIndex);
        return T.Read(this, ref reader);
    }

    public T EnumParse<T>(DataCoreStringId stringId, T unknown) where T : struct, Enum
    {
        var value = stringId.ToString(Database);
        
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

    public T?[] ReadReferenceArray<T>(ref SpanReader reader) where T : class, IDataCoreReadable<T>
    {
        var count = reader.ReadInt32();
        var firstIndex = reader.ReadInt32();

        var array = new T?[count];

        for (var i = firstIndex; i < firstIndex + count; i++)
        {
            array[i - firstIndex] = ReadFromReference<T>(Database.ReferenceValues[i]);
        }

        return array;
    }

    public T[] ReadWeakPointerArray<T>(ref SpanReader reader)
        //where T  : IDataCoreReadable<T>
    {
        var count = reader.ReadInt32();
        var firstIndex = reader.ReadInt32();

        var array = new T[count];

        for (var i = firstIndex; i < firstIndex + count; i++)
        {
            //TODO: causes recursive loop
            //array[i - firstIndex] = ReadFromPointer<T>(db, Database.WeakValues[i]);
            array[i - firstIndex] = default;
        }

        return array;
    }

    public Lazy<T>[] ReadWeakPointerArrayLazy<T>(ref SpanReader reader) where T : class, IDataCoreReadable<T>
    {
        var count = reader.ReadInt32();
        var firstIndex = reader.ReadInt32();

        var array = new Lazy<T>[count];

        for (var i = firstIndex; i < firstIndex + count; i++)
        {
            var ptr = Database.WeakValues[i];
            array[i - firstIndex] = new Lazy<T>(() => ReadFromPointer<T>(ptr));
        }

        return array;
    }

    public T?[] ReadStrongPointerArray<T>(ref SpanReader reader) where T : class, IDataCoreReadable<T>
    {
        var count = reader.ReadInt32();
        var firstIndex = reader.ReadInt32();

        var array = new T?[count];

        for (var i = firstIndex; i < firstIndex + count; i++)
        {
            var strongValue = Database.StrongValues[i];
            var read = ReadFromRecord(strongValue.StructIndex, strongValue.InstanceIndex);
            if (read == null)
                array[i - firstIndex] = null!;
            else if (read is T readable)
                array[i - firstIndex] = readable;
            else
                throw new Exception($"ReadFromPointer failed to cast {read.GetType()} to {typeof(T)}");
        }

        return array;
    }

    public T[] ReadClassArray<T>(ref SpanReader reader, int structIndex) where T : class, IDataCoreReadable<T>
    {
        var count = reader.ReadInt32();
        var firstIndex = reader.ReadInt32();

        var array = new T[count];

        for (var i = firstIndex; i < firstIndex + count; i++)
        {
            array[i - firstIndex] = ReadFromInstance<T>(structIndex, i)
                                    ?? throw new Exception($"ReadFromInstance failed to read instance of {typeof(T)}");
        }

        return array;
    }

    public T[] ReadEnumArray<T>(ref SpanReader reader) where T : struct, Enum
    {
        var count = reader.ReadInt32();
        var firstIndex = reader.ReadInt32();

        var array = new T[count];

        for (var i = firstIndex; i < firstIndex + count; i++)
        {
            array[i - firstIndex] = EnumParse<T>(Database.EnumValues[i], default);
        }

        return array;
    }

    public string[] ReadStringArray(ref SpanReader reader)
    {
        var count = reader.ReadInt32();
        var firstIndex = reader.ReadInt32();
        var result = new string[count];

        for (var i = 0; i < count; i++)
            result[i] = Database.StringIdValues[firstIndex + i].ToString(Database);

        return result;
    }

    public string[] ReadLocaleArray(ref SpanReader reader)
    {
        var count = reader.ReadInt32();
        var firstIndex = reader.ReadInt32();
        var result = new string[count];

        for (var i = 0; i < count; i++)
            result[i] = Database.LocaleValues[firstIndex + i].ToString(Database);

        return result;
    }

    public sbyte[] ReadSByteArray(ref SpanReader reader)
    {
        var count = reader.ReadInt32();
        var firstIndex = reader.ReadInt32();
        return Database.Int8Values.AsSpan(firstIndex, count).ToArray();
    }

    public short[] ReadInt16Array(ref SpanReader reader)
    {
        var count = reader.ReadInt32();
        var firstIndex = reader.ReadInt32();
        return Database.Int16Values.AsSpan(firstIndex, count).ToArray();
    }

    public int[] ReadInt32Array(ref SpanReader reader)
    {
        var count = reader.ReadInt32();
        var firstIndex = reader.ReadInt32();
        return Database.Int32Values.AsSpan(firstIndex, count).ToArray();
    }

    public long[] ReadInt64Array(ref SpanReader reader)
    {
        var count = reader.ReadInt32();
        var firstIndex = reader.ReadInt32();
        return Database.Int64Values.AsSpan(firstIndex, count).ToArray();
    }

    public byte[] ReadByteArray(ref SpanReader reader)
    {
        var count = reader.ReadInt32();
        var firstIndex = reader.ReadInt32();
        return Database.UInt8Values.AsSpan(firstIndex, count).ToArray();
    }

    public ushort[] ReadUInt16Array(ref SpanReader reader)
    {
        var count = reader.ReadInt32();
        var firstIndex = reader.ReadInt32();
        return Database.UInt16Values.AsSpan(firstIndex, count).ToArray();
    }

    public uint[] ReadUInt32Array(ref SpanReader reader)
    {
        var count = reader.ReadInt32();
        var firstIndex = reader.ReadInt32();
        return Database.UInt32Values.AsSpan(firstIndex, count).ToArray();
    }

    public ulong[] ReadUInt64Array(ref SpanReader reader)
    {
        var count = reader.ReadInt32();
        var firstIndex = reader.ReadInt32();
        return Database.UInt64Values.AsSpan(firstIndex, count).ToArray();
    }

    public bool[] ReadBoolArray(ref SpanReader reader)
    {
        var count = reader.ReadInt32();
        var firstIndex = reader.ReadInt32();
        return Database.BooleanValues.AsSpan(firstIndex, count).ToArray();
    }

    public float[] ReadSingleArray(ref SpanReader reader)
    {
        var count = reader.ReadInt32();
        var firstIndex = reader.ReadInt32();
        return Database.SingleValues.AsSpan(firstIndex, count).ToArray();
    }

    public double[] ReadDoubleArray(ref SpanReader reader)
    {
        var count = reader.ReadInt32();
        var firstIndex = reader.ReadInt32();
        return Database.DoubleValues.AsSpan(firstIndex, count).ToArray();
    }
}