namespace StarBreaker.P4k;

public sealed class GenericFileProcessor : IFileProcessor
{
    public bool CanProcess(string entryName, Stream stream) => true;

    public void ProcessEntry(string outputRootFolder, string entryName, Stream entryStream)
    {
        var entryPath = Path.Combine(outputRootFolder, entryName);

        using var writeStream = new FileStream(entryPath, FileMode.Create, FileAccess.Write, FileShare.None);

        entryStream.CopyTo(writeStream);
    }
}