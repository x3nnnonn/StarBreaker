using System.IO.Compression;

namespace StarBreaker.SocPak;

public sealed class SocPakFileNode : ISocPakNode
{
    public ISocPakNode? Parent { get; }
    public string Name { get; }
    public ZipArchiveEntry ZipEntry { get; }
    public string FullPath => ZipEntry.FullName;
    public long Size => ZipEntry.Length;
    public DateTime LastModified => ZipEntry.LastWriteTime.DateTime;

    public SocPakFileNode(ZipArchiveEntry zipEntry, ISocPakNode? parent = null)
    {
        ZipEntry = zipEntry;
        Name = Path.GetFileName(zipEntry.FullName);
        Parent = parent;
    }
} 