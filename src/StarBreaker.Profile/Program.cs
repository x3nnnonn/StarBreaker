using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Numerics;
using System.Text;
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
        var dict = new ConcurrentDictionary<uint, HashSet<string>>();
        var tested = 0;

        var uintsToTest = ReadKeys("keys.txt");

        var forge = new DataForge(@"D:\out\Data\Game.dcb");
        var enums = forge.ExportEnums();

        IEnumerable<string> haystack = new List<string>();

        haystack = haystack.Concat(forge._database.EnumerateStrings1().Concat(forge._database.EnumerateStrings2()));
        haystack = haystack.Concat(["head_eyedetail"]);
        haystack = haystack.Concat(enums.Select(x => x.Key));
        haystack = haystack.Concat(enums.SelectMany(x => x.Value));
        haystack = haystack.Concat(StreamLines("strings.txt"));
        //haystack = haystack.Concat(StreamLines(@"D:\New folder\oof2.txt"));
        haystack = haystack.Concat(StreamLines("mats.txt"));
        haystack = haystack.Concat(StreamLines("working.txt"));
        haystack = haystack.Concat(Directory.EnumerateFiles(@"D:\out", "*", SearchOption.AllDirectories).Select(Path.GetFileNameWithoutExtension));
        haystack = haystack.SelectMany(GetVariations);

        var result = BruteForce(uintsToTest, haystack);

        foreach (var (key, value) in result.OrderBy(x => x.Value.Count))
        {
            Console.WriteLine($"0x{key:X8} [{string.Join(", ", value)}]");
        }

        Console.WriteLine($"Number of found keys: {result.Values.Count(x => x.Count > 0)}");
        Console.WriteLine($"Number of missing keys: {result.Values.Count(x => x.Count == 0)}");
        Console.WriteLine($"Number of tested strings: {tested}");
        return;

        IEnumerable<string> GetVariations(string str)
        {
            foreach (var s in str.Split('/'))
            {
                yield return s;
            }
            
            foreach (var s in str.Split('_'))
            {
                yield return s;
            }
            
            foreach (var s in str.Split('-'))
            {
                yield return s;
            }
            
            foreach (var s in str.Split(' '))
            {
                yield return s;
            }
            
            yield return str.ToLower();
            yield return str.ToUpper();

            //return this last so it gets replaced in the dictionary if it matches
            yield return str;
        }

        IEnumerable<string> GetNumbered(string str)
        {
            yield return str;
            for (var i = 0; i < 2; i++)
            {
                yield return $"{str}{i}";
            }
        }

        uint[] ReadKeys(string file)
        {
            var lines = File.ReadAllLines(file);
            var keys = new List<uint>();

            foreach (var line in lines)
            {
                if (line.StartsWith("0x") &&
                    uint.TryParse(line[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var key))
                {
                    keys.Add(key);
                }
            }

            return keys.Distinct().Order().ToArray();
        }
    }

    private static IEnumerable<string> StreamLines(string filePath)
    {
        using var reader = new StreamReader(File.OpenRead(filePath));

        while (reader.ReadLine() is { } line)
            yield return line;
    }

    /// <summary>
    ///     Brute force all possible combinations of strings and keys
    /// </summary>
    /// <param name="keys">crc32c results to test</param>
    /// <param name="strings">Original strings to modify and test against</param>
    private static ConcurrentDictionary<uint, HashSet<string>> BruteForce(uint[] keys, IEnumerable<string> strings)
    {
        var dict = new ConcurrentDictionary<uint, HashSet<string>>(keys.ToDictionary(key => key, _ => new HashSet<string>()));
        var tested = 0;

        var buffer = new byte[4096];

        foreach (var str in strings)
        {
            Interlocked.Increment(ref tested);
            var byteLength = Encoding.ASCII.GetBytes(str, buffer);
            var acc = 0xFFFFFFFFu;

            for (var i = 0; i < byteLength; i++)
            {
                acc = BitOperations.Crc32C(acc, buffer[i]);
                var crc = ~acc;
                if (keys.Contains(crc))
                {
                    dict[crc].Add(str[..(i + 1)]);
                }
            }

            // for (var i = byteLength - 1; i >= 0; i--)
            // {
            //     acc = 0xFFFFFFFFu;
            //     for (var j = 0; j < byteLength; j++)
            //     {
            //         if (j == i) continue;
            //         acc = BitOperations.Crc32C(acc, buffer[j]);
            //     }
            //
            //     var crc = ~acc;
            //     if (keys.Contains(crc))
            //     {
            //         dict[crc].Add(str.Substring(0, i));
            //     }
            // }
        }

        return dict;
    }
}