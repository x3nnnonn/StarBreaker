namespace StarBreaker.P4k;

public interface IFileProcessor
{
    bool CanProcess(string entryName, Stream stream);
    void ProcessEntry(string outputRootFolder, string entryName, Stream entryStream);
}