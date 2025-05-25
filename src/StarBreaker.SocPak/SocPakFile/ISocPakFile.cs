using System.IO.Compression;

namespace StarBreaker.SocPak;

public interface ISocPakFile
{
    string SocPakPath { get; }
    ZipArchiveEntry[] Entries { get; }
    SocPakDirectoryNode Root { get; }
    Stream OpenStream(ZipArchiveEntry entry);
} 