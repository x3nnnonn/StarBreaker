using StarBreaker.CryXmlB;

namespace StarBreaker.P4k;

public sealed class CryXmlProcessor : IFileProcessor
{
    public bool CanProcess(string entryName, Stream stream)
    {
        if (stream.Length < 4)
            return false;

        Span<byte> test = stackalloc byte[4];
        if (stream.Read(test) != 4)
            return false;
        
        return CryXml.IsCryXmlB(test);
    }

    public void ProcessEntry(string outputRootFolder, string entryName, Stream stream)
    {
        throw new NotImplementedException();
    }
}