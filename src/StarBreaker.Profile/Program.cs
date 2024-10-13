using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using Humanizer;
using StarBreaker.Common;
using StarBreaker.CryChunkFile;
using StarBreaker.CryXmlB;
using StarBreaker.Forge;
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
        //TimeP4kExtract();
        //ExtractChunkFiles();
        FindStringCrc32();
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

    private static void FindStringCrc32()
    {
        var uintsToTest = File.ReadAllLines("keys.txt").Select(z => uint.Parse(z[2..], NumberStyles.HexNumber)).Distinct().ToArray();
        
        var forge = new DataForge(@"D:\out\Data\Game.dcb");
        var enums = forge.ExportEnums();
        List<string> stringsToTest = new();
        stringsToTest.AddRange(File.ReadAllLines("strings.txt").Where(x => x.Length > 4));
        stringsToTest.AddRange(forge._database.EnumerateStrings1());
        stringsToTest.AddRange(forge._database.EnumerateStrings2());
        stringsToTest.AddRange(enums.Select(x => x.Key));
        stringsToTest.AddRange(enums.SelectMany(x => x.Value));
        foreach (var (enumName, values) in enums)
        {
            stringsToTest.AddRange(values.Select(x => $"{enumName}.{x}"));
        }

        var tested = 0;
        var dict = new ConcurrentDictionary<uint, List<string>>();

        var haystack = stringsToTest.SelectMany(GetVariations).ToArray();
        var dangerous = haystack.SelectMany(x => haystack, (x, y) => $"{x}{y}");
        
        foreach (var str in haystack.AsParallel())
        {
            Interlocked.Increment(ref tested);
            var crc = Crc32c.FromString(str);
            if (uintsToTest.Contains(crc))
            {
                if (dict.TryGetValue(crc, out var list))
                {
                    list.Add(str);
                }
                else
                {
                    dict.TryAdd(crc, [str]);
                }
            }

            if (tested % 5000000 == 0)
            {
                Console.WriteLine($"Tested {tested} strings with {dict.Count} matches");
                foreach (var (key, value) in dict)
                {
                    Console.WriteLine($"0x{key:X8} [{string.Join(", ", value)}]");
                }
            }
        }
        
        var missing = uintsToTest.Except(dict.Keys).ToArray();
        
        foreach (var key in missing)
        {
            Console.WriteLine($"Missing 0x{key:x8}");
        }
        
        foreach (var (key, value) in dict)
        {
            Console.WriteLine($"0x{key:X8} [{string.Join(", ", value)}]");
        }
        
        Console.WriteLine($"Tested {tested} strings");
        Console.WriteLine($"Found {dict.Count} matches");
        Console.WriteLine($"Missing {missing.Length} matches");

        return;

        IEnumerable<string> GetVariations(string str)
        {
            yield return str.ToLower();
            // yield return str.ToUpper();
            // yield return str.Humanize();
            // yield return str.Dehumanize();
            //return this last so it gets replaced in the dictionary if it matches
            yield return str;
        }

        IEnumerable<string> GetNumbered(string str)
        {
            yield return str;
            for (var i = 0; i < 10; i++)
            {
                yield return $"{str}{i}";
            }
        }
    }
}