using System.Diagnostics;
using StarBreaker.CryXmlB;
using StarBreaker.P4k;

namespace StarBreaker.Profile;

public static class Program
{
    public static void Main(string[] args)
    {
        const string dest = @"D:\xml-parsed";
        const string p4k = @"C:\Program Files\Roberts Space Industries\StarCitizen\LIVE\Data.p4k";
        var sw1 = Stopwatch.StartNew();

        var p4kf = new P4kFile(p4k);
        var elapsed1 = sw1.ElapsedMilliseconds;
        Console.WriteLine($"P4kFile ctor: {elapsed1}ms");
        return;
        
        
        var xmlFiles = Directory.GetFiles("D:\\out", "*.xml", SearchOption.AllDirectories);
        var sw = Stopwatch.StartNew();
        Parallel.ForEach(xmlFiles, file =>
        {
            if (!CryXml.TryOpen(File.ReadAllBytes(file), out var cryXml))
                return;

            var destFile = file.Replace("D:\\out", dest);
            Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
            using var textWriter1 = new StreamWriter(destFile);
            cryXml.WriteXml(textWriter1);
        });
        var elapsed = sw.ElapsedMilliseconds;

        Console.WriteLine($"Elapsed: {elapsed}ms");
    }
}