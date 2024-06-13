using System.Diagnostics;
using StarBreaker.Forge;

namespace StarBreaker;

public static class Program
{
    private const string dcb = @"C:\Scratch\extract\Game.dcb";
    private const string destDir = @"C:\Scratch\extract\stbr\";
    private static DataForge dataForge = null!;

    public static void Main()
    {
        var bytes = File.ReadAllBytes(dcb);

        var sw1 = Stopwatch.StartNew();
        dataForge = new DataForge(bytes, destDir);
        Console.WriteLine($"DataForge Constructor took: {sw1.ElapsedMilliseconds}ms");

        var printed1 = -1;
        var exportProgress = new Progress<float>(percent =>
        {
            if (Math.Abs(percent % 10) < 1)
            {
                var part = (int)percent / 10;
                if (part > printed1)
                {
                    printed1 = part;
                    Console.WriteLine($"Export {percent}%");
                }
            }
        });
        var sw2 = Stopwatch.StartNew();
        dataForge.Export(null, exportProgress);
        Console.WriteLine($"Export took: {sw2.ElapsedMilliseconds}ms");

        int printed = -1;
        var exportProgress1 = new Progress<float>(percent =>
        {
            if (Math.Abs(percent % 10) < 1)
            {
                var part = (int)percent / 10;
                if (part > printed)
                {
                    printed = part;
                    Console.WriteLine($"ExportSingle {percent}%");
                }
            }
        });
        var sw = Stopwatch.StartNew();
        dataForge.ExportSingle(null, exportProgress1);
        Console.WriteLine($"ExportSingle took: {sw.ElapsedMilliseconds}ms");

        // foreach (var x in dataForge.ExportEnums())
        // {            
        //     File.WriteAllText(Path.Combine(destDir, Path.ChangeExtension(x.Key, ".txt")), string.Join("\n", x.Value));
        // }
    }
}