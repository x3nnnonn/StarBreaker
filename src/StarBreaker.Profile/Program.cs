using System.Diagnostics;
using System.IO.Compression;
using StarBreaker.CryChunkFile;
using StarBreaker.CryXmlB;
using StarBreaker.P4k;

// ReSharper disable UnusedMember.Local
namespace StarBreaker.Profile;

public static class Program
{
    private const string p4k = @"C:\Program Files\Roberts Space Industries\StarCitizen\LIVE\Data.p4k";
    private const string socpak = @"D:\StarCitizenExport\Data\ObjectContainers\PU\loc\flagship\stanton\newbab\newbab_all.socpak";
    private const string depot = @"D:\StarCitizenExport";
    private const string problematic = @"D:\newbab_all.rmxml";

    public static void Main(string[] args)
    {
        //var cxml = new CryXml(File.ReadAllBytes(problematic));
        //File.WriteAllText("output.xml", cxml.ToXmlString());

        //ExtrackSocPak();
        TimeP4kExtract();
        //ExtractChunkFiles();
    }

    private static void ExtractChunkFiles()
    {
        var exts = new[]
        {
            "aim",
            "caf",
            "chr",
            "dba",
            "img",
            "skin",
            "skinm",
            "cga",
            "cgam",
            "cgf",
            "cgfm",
            "cigvoxel",
            "cigvoxelheader",
            "dst",
            "soc",
        };

        var filteredFiles = Directory.EnumerateFiles(depot, "*", SearchOption.AllDirectories)
            .Where(file => exts.Any(ext => file.EndsWith(ext, StringComparison.InvariantCultureIgnoreCase)));

        foreach (var file in filteredFiles)
        {
            try
            {
                if (!ChunkFile.TryOpen(File.ReadAllBytes(file), out var chunkFile))
                    throw new Exception("Failed to open chunk file");

                chunkFile!.WriteXmlTo(Console.Out);
            }
            catch (Exception e)
            {
                Console.WriteLine(file);
                Console.WriteLine(e);
            }
        }
    }
    
    private static void ExtrackSocPak()
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

    private static void TimeP4kExtract()
    {
        var sw1 = Stopwatch.StartNew();
        var p4kFile = new P4kFile(p4k);
        sw1.Stop();
        
        Console.WriteLine($"Took {sw1.ElapsedMilliseconds}ms to load {p4kFile.Entries.Length} entries");
    }
    
    private static void TimeZipNode()
    {        
        var p4kFile = new P4kFile(p4k);

        var times = new List<long>();
        for (var i = 0; i < 8; i++)
        {
            var sw = Stopwatch.StartNew();
            var tree = new ZipNode(p4kFile.Entries);
            times.Add(sw.ElapsedMilliseconds);
        }

        Console.WriteLine($"Average: {times.Average()}ms");
    }
}