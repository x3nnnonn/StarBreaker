using System.Diagnostics;
using StarBreaker.P4k;

namespace StarBreaker.Profile;

public static class Program
{
    public static void Main(string[] args)
    {
        var sw = Stopwatch.StartNew();
        var xxx = new DirectP4kReader(@"D:\extract\Data.p4k");
        sw.Stop();

        Console.WriteLine($"Load time: {sw.ElapsedMilliseconds}ms");

        xxx.Extract(@"C:\Scratch\pog");
    }
}