using System.Diagnostics;
using StarBreaker.P4k;

namespace StarBreaker.Sandbox;

public static class TimeP4kExtract
{
    private const string p4k = @"C:\Program Files\Roberts Space Industries\StarCitizen\LIVE\Data.p4k";

    public static void Run()
    {
        var sw1 = Stopwatch.StartNew();
        var p4kFile = new P4kFile(p4k);
        sw1.Stop();

        Console.WriteLine($"Took {sw1.ElapsedMilliseconds}ms to load {p4kFile.Entries.Length} entries");
    }
}