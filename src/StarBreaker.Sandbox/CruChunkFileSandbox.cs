using StarBreaker.CryChunkFile;

namespace StarBreaker.Sandbox;

public static class CruChunkFileSandbox
{
    private const string depot = @"C:\Scratch\StarCitizen\p4k";

    public static void Run()
    {
        var exts = new[]
        {
            // "aim",
            // "caf",
            // "chr",
            // "dba",
            // "img",
            // "skin",
            // "skinm",
            "cga",
            "cgam",
            //"cgf",
            // "cgfm",
            // "cigvoxel",
            // "cigvoxelheader",
            // "dst",
            // "soc",
        };

        var filteredFiles = Directory.EnumerateFiles(depot, "*", SearchOption.AllDirectories)
            .Where(file => exts.Any(ext => file.EndsWith(ext, StringComparison.InvariantCultureIgnoreCase)));

        foreach (var file in filteredFiles)
        {
           var  file2 = @"C:\Scratch\StarCitizen\p4k\Data\Objects\Spaceships\Ships\AEGS\Gladius\AEGS_Gladius.cgam";
            try
            {
                if (!ChunkFile.TryOpen(File.ReadAllBytes(file2), out var chunkFile))
                    throw new Exception("Failed to open chunk file");

                chunkFile!.WriteXmlTo(Console.Out);
            }
            catch (Exception e)
            {
                Console.WriteLine(file2);
                Console.WriteLine(e);
            }
        }
    }
}