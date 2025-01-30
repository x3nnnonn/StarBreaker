using System.Runtime.InteropServices;
using StarBreaker.Common;

namespace StarBreaker.DataCore;

/// <summary>
///     Walks a DataCore record and extracts all the weak pointers.
/// </summary>
public static class DataCoreBinaryWalker
{
    public static Dictionary<(int, int), int> WalkRecord(DataCoreRecord record, DataCoreDatabase database)
    {
        var context = new Context { Database = database, WeakPointers = [] };

        WalkInstance(record.StructIndex, record.InstanceIndex, context);

        return context.WeakPointers;
    }

    private static void WalkInstance(int structIndex, int instanceIndex, Context context)
    {
        var reader = context.Database.GetReader(structIndex, instanceIndex);

        WalkStruct(structIndex, ref reader, context);
    }

    private static void WalkStruct(int structIndex, ref SpanReader reader, Context context)
    {
        foreach (var prop in context.Database.GetProperties(structIndex))
        {
            if (prop.ConversionType == ConversionType.Attribute)
                WalkAttribute(prop, ref reader, context);
            else
                WalkArray(prop, ref reader, context);
        }
    }

    private static void WalkAttribute(DataCorePropertyDefinition prop, ref SpanReader reader, Context context)
    {
        switch (prop.DataType)
        {
            case DataType.Reference: WalkReference(reader.Read<DataCoreReference>(), context); break;
            case DataType.WeakPointer: WalkWeakPointer(reader.Read<DataCorePointer>(), context); break;
            case DataType.StrongPointer: WalkStrongPointer(reader.Read<DataCorePointer>(), context); break;
            case DataType.Class: WalkStruct(prop.StructIndex, ref reader, context); break;
            // we don't care about the value, just advance the reader
            default: reader.Advance(prop.DataType.GetSize()); break;
        }
    }

    private static void WalkArray(DataCorePropertyDefinition prop, ref SpanReader reader, Context context)
    {
        var count = reader.ReadInt32();
        var firstIndex = reader.ReadInt32();

        for (var i = firstIndex; i < firstIndex + count; i++)
        {
            switch (prop.DataType)
            {
                case DataType.Reference: WalkReference(context.Database.ReferenceValues[i], context); break;
                case DataType.WeakPointer: WalkWeakPointer(context.Database.WeakValues[i], context); break;
                case DataType.StrongPointer: WalkStrongPointer(context.Database.StrongValues[i], context); break;
                case DataType.Class: WalkInstance(prop.StructIndex, i, context); break;
            }
        }
    }

    private static void WalkReference(DataCoreReference reference, Context context)
    {
        if (reference.RecordId == CigGuid.Empty || reference.InstanceIndex == -1)
            return;

        var record = context.Database.GetRecord(reference.RecordId);

        if (context.Database.MainRecords.Contains(reference.RecordId))
            return;

        WalkInstance(record.StructIndex, record.InstanceIndex, context);
    }

    private static void WalkStrongPointer(DataCorePointer strongPointer, Context context)
    {
        if (strongPointer.StructIndex == -1 || strongPointer.InstanceIndex == -1)
            return;

        WalkInstance(strongPointer.StructIndex, strongPointer.InstanceIndex, context);
    }

    private static void WalkWeakPointer(DataCorePointer weakPointer, Context context)
    {
        if (weakPointer.StructIndex == -1 || weakPointer.InstanceIndex == -1)
            return;

        context.TryAddWeakPointer(weakPointer.StructIndex, weakPointer.InstanceIndex);
    }

    private readonly struct Context
    {
        public required DataCoreDatabase Database { get; init; }
        public required Dictionary<(int, int), int> WeakPointers { get; init; }

        public void TryAddWeakPointer(int structIndex, int instanceIndex)
        {
            ref var id = ref CollectionsMarshal.GetValueRefOrAddDefault(WeakPointers, (structIndex, instanceIndex), out var existed);
            if (!existed)
                id = WeakPointers.Count;
        }
    }
}