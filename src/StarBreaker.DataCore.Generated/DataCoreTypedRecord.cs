using StarBreaker.Common;

namespace StarBreaker.DataCoreGenerated;

public record DataCoreTypedRecord(
    string RecordFileName,
    string RecordName,
    CigGuid RecordId,
    IDataCoreReadable Data
);