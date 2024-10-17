namespace StarBreaker.P4k;

public interface IFileProcessor
{
    bool CanProcess(string entryName, Stream stream);
    void ProcessEntry(string outputRootFolder, string entryName, Stream entryStream);
}

public static class FileProcessors
{
    private static readonly GenericFileProcessor _genericFileProcessor = new();
    public static List<IFileProcessor> Processors { get; }
    
    static FileProcessors()
    {
        //reflection would be nicer but native aot doesn't support it
        Processors =
        [
            new CryXmlProcessor(),
            new ZipFileProcessor(),
        ];
    }
    
    public static IFileProcessor GetProcessor(string entryName, Stream entryStream)
    {
        if (Processors.FirstOrDefault(p => p.CanProcess(entryName, entryStream)) is { } processor)
            return processor;
        
        return _genericFileProcessor;
    }
}