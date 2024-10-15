using System.IO.Compression;
using StarBreaker.CryXmlB;

namespace StarBreaker.Sandbox;

public static class ExtractSocPak
{
    private const string socpak = @"C:\Scratch\StarCitizen\p4k\Data\ObjectContainers\PU\loc\flagship\stanton\newbab\newbab_all.socpak";

    public static void Run()
    {
        using var zip = new ZipArchive(File.OpenRead(socpak));
        foreach (var entry in zip.Entries)
        {
            var stream = entry.Open();
            var bytes = new byte[entry.Length];
            stream.Read(bytes);
            if (CryXml.TryOpen(bytes, out var cryXml))
            {
                var s = cryXml.ToXmlString();
                Console.WriteLine(cryXml);
            }

            Console.WriteLine(entry.FullName);
        }
    }
}