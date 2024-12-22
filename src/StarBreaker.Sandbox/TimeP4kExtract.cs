using System.Diagnostics;
using System.IO.Compression;
using StarBreaker.P4k;

namespace StarBreaker.Sandbox;

public static class TimeP4kExtract
{
    private const string p4k = @"C:\Program Files\Roberts Space Industries\StarCitizen\4.0_PREVIEW\Data.p4k";

    public static void Run()
    {
        var sw = Stopwatch.StartNew();
        var p4kFile = P4kFile.FromFile(p4k);
        Console.WriteLine($"Took {sw.ElapsedMilliseconds}ms to load {p4kFile.Entries.Length} entries");
    }
}