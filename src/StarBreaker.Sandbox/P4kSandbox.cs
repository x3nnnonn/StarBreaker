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
        //Verify();
        //ListByExtension();
        CountEncrypted();
    }
    
    private static void ListByExtension()
    {
        var sw = Stopwatch.StartNew();
        var p4kFile = P4kFile.FromFile(p4k);

        var ordered = p4kFile.Entries
            .Where(x => x.UncompressedSize > 0)
            .OrderBy(x => x.Offset);

        var failed = new List<ZipEntry>();

        var ivo = BitConverter.ToUInt32("#ivo"u8);
        var crch = BitConverter.ToUInt32("CrCh"u8);
        var exts = new Dictionary<string, int>();

        var cnt = 0;
        Parallel.ForEach(ordered, entry =>
        {
            try
            {
                using var bytes = p4kFile.OpenStream(entry);
                var binaryreader = new BinaryReader(bytes);
                var uint32 = binaryreader.ReadUInt32();
                if (uint32 == ivo || uint32 == crch)
                {
                    lock (exts)
                    {
                        var ext = Path.GetExtension(entry.Name);
                        if (!exts.TryAdd(ext, 1))
                        {
                            exts[ext]++;
                        }
                    }
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
    
    private static void CountEncrypted()
    {
        var sw = Stopwatch.StartNew();
        var p4kFile = P4kFile.FromFile(p4k);
        
        var uncompressed = p4kFile.Entries.Count(x => x.CompressionMethod == 0);
        var compressed = p4kFile.Entries.Count(x => x.CompressionMethod == 100 && x.IsCrypted == false);
        var encrypted = p4kFile.Entries.Count(x => x.IsCrypted == true);
        
        var encrypted2 = p4kFile.Entries.Where(x => x.IsCrypted).OrderBy(x => x.UncompressedSize).ToList();
        
        
        var total = uncompressed + compressed + encrypted;
        
        Console.WriteLine($"Uncompressed: {uncompressed}, {uncompressed / (double)total * 100}%");
        Console.WriteLine($"Compressed: {compressed}, {compressed / (double)total * 100}%");
        Console.WriteLine($"Encrypted: {encrypted}, {encrypted / (double)total * 100}%");
        
        Console.WriteLine($"Took {sw.ElapsedMilliseconds}ms to load {p4kFile.Entries.Length} entries");
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