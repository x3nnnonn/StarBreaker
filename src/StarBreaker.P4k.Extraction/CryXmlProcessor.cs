using StarBreaker.CryXmlB;

namespace StarBreaker.P4k;

public sealed class CryXmlProcessor : IFileProcessor
{
    public bool CanProcess(string entryName, Stream stream)
    {
        Span<byte> test = stackalloc byte[4];
        var read = stream.Read(test);
        stream.Seek(0, SeekOrigin.Current);

        if (read != 4)
            return false;

        return CryXml.IsCryXmlB(test);
    }

    public void ProcessEntry(string outputRootFolder, string entryName, Stream entryStream)
    {
        throw new NotImplementedException();
    }
}