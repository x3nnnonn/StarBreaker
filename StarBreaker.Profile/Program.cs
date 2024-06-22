using System.Diagnostics;
using StarBreaker.CryXmlB;

namespace StarBreaker.Profile;

public static class Program
{
    public static void Main(string[] args)
    {
        const string dest = "D:\\xml-parsed";
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