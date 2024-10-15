using System.Collections.Concurrent;
using System.Globalization;
using System.Numerics;
using System.Text;
using StarBreaker.Forge;

namespace StarBreaker.Sandbox;

public static class StringCrc32c
{
    public static void Run()
    {
        var dict = new ConcurrentDictionary<uint, HashSet<string>>();
        var tested = 0;

        var uintsToTest = ReadKeys("keys.txt");

        var forge = new DataForge(@"C:\Scratch\StarCitizen\p4k\Data\Game.dcb");
        var enums = forge.ExportEnums();

        IEnumerable<string> haystack = new List<string>();

        haystack = haystack.Concat(forge._database.EnumerateStrings1().Concat(forge._database.EnumerateStrings2()));
        haystack = haystack.Concat(["head_eyedetail"]);
        haystack = haystack.Concat(enums.Select(x => x.Key));
        haystack = haystack.Concat(enums.SelectMany(x => x.Value));
        //haystack = haystack.Concat(StreamLines("strings.txt"));
        //haystack = haystack.Concat(StreamLines(@"D:\New folder\oof2.txt"));
        //haystack = haystack.Concat(StreamLines("mats.txt"));
        //haystack = haystack.Concat(StreamLines("working.txt"));

        haystack = haystack.Concat(Directory.EnumerateFiles(@"C:\Scratch\StarCitizen\p4k", "*", SearchOption.AllDirectories).Select(Path.GetFileNameWithoutExtension));
        haystack = haystack.SelectMany(GetVariations);
        //TODO: charactercustomizer_pu.socpak

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