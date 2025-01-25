namespace StarBreaker.DataCore;

public class DataCoreExtractionOptions
{
    public required bool ShouldWriteMetadata { get; init; }
    public required bool ShouldWriteTypeNames { get; init; }
    public required bool ShouldWriteBaseTypeNames { get; init; }
    public required bool ShouldWriteEnumMetadata { get; init; }
    public required bool ShouldSkipEmptyArrays { get; init; }
}