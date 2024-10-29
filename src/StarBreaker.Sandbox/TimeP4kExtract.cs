using System.Diagnostics;
using StarBreaker.P4k;

namespace StarBreaker.Sandbox;

public static class TimeP4kExtract
{
    private const string p4k = @"C:\Program Files\Roberts Space Industries\StarCitizen\LIVE\Data.p4k";

    public static void Run()
    {
        var sw1 = Stopwatch.StartNew();
        var p4kFile = P4kFile.FromFile(p4k);
        Console.WriteLine($"Took {sw1.ElapsedMilliseconds}ms to load {p4kFile.Entries.Length} entries");

        sw1.Restart();
        Array.Sort(p4kFile.Entries, static (a, b) => a.Offset.CompareTo(b.Offset));

        sw1.Stop();

        Console.WriteLine($"Took {sw1.ElapsedMilliseconds}ms to load {p4kFile.Entries.Length} entries");
    }
}