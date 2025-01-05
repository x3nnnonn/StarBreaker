using System.Diagnostics;
using System.IO.Compression;

namespace StarBreaker.Sandbox;

//creates a dummy zip with the same structure as a star citizen p4k file, for testing purposes
public static class ZipFileSandbox
{
    public static void Run()
    {
        using var zip = new ZipArchive(File.Create("test.zip"), ZipArchiveMode.Create);

        foreach (var file in Directory.EnumerateFiles(@"D:\StarCitizen\P4k", "*", SearchOption.AllDirectories))
        {
            var relativePath = file[@"D:\StarCitizen\P4k\".Length..];
            var entry = zip.CreateEntry(relativePath, CompressionLevel.NoCompression);
            using var stream = entry.Open();
            stream.WriteByte(0);
        }
    }
}