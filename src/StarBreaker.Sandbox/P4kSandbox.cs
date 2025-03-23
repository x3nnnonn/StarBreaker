using System.Diagnostics;
using System.IO.Compression;
using StarBreaker.Common;
using StarBreaker.P4k;

namespace StarBreaker.Sandbox;

public static class P4kSandbox
{
    private const string p4k = @"C:\Program Files\Roberts Space Industries\StarCitizen\PTU\Data.p4k";

    public static void Run()
    {
        Verify();
    }

    private static void Verify()
    {
        var sw = Stopwatch.StartNew();
        var p4kFile = P4kFile.FromFile(p4k);

        var ordered = p4kFile.Entries.OrderBy(x => x.Offset);

        var failed = new List<ZipEntry>();

        var cnt = 0;
        Parallel.ForEach(ordered, entry =>
        {
            try
            {
                using var bytes = p4kFile.OpenStream(entry);
                var crc = Crc32c.FromStream(bytes);
                var matches = entry.Crc32 == crc;
                if (!matches)
                {
                    Console.WriteLine($"CRC32 mismatch for {entry.Name}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to open {entry.Name}");
                failed.Add(entry);
            }

            Interlocked.Increment(ref cnt);

            if (cnt % 10000 == 0)
            {
                Console.WriteLine($"Processed {cnt} entries({(cnt / (double)p4kFile.Entries.Length) * 100}%)");
            }
        });

        foreach (var entry in failed)
        {
            Console.WriteLine($"Failed to open {entry.Name}");
        }

        Console.WriteLine($"Took {sw.ElapsedMilliseconds}ms to load {p4kFile.Entries.Length} entries");
    }
}