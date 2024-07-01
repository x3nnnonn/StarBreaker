using StarBreaker.CryChunkFile;

namespace StarBreaker.Profile;

public static class Program
{
    public static void Main(string[] args)
    {
        const string p4k = @"C:\Program Files\Roberts Space Industries\StarCitizen\LIVE\Data.p4k";
        const string depot = @"D:\StarCitizenExport";

        var exts = new[]
        {
            "aim",
            "caf",
            "chr",
            "dba",
            "img",
            "skin",
            "skinm",
            "cga",
            "cgam",
            "cgf",
            "cgfm",
            "cigvoxel",
            "cigvoxelheader",
            "dst",
            "soc",
        };

        var filteredFiles = Directory.EnumerateFiles(depot, "*", SearchOption.AllDirectories)
            .Where(file => exts.Any(ext => file.EndsWith(ext, StringComparison.InvariantCultureIgnoreCase)));
            // .Where(ChunkFile.IsChunkFile)
            // .Select(x => x.Split('.').Last()).Distinct().ToArray();

        foreach (var file in filteredFiles)
        {
            try
            {
                ExtractChunkFile(file);
            }
            catch (Exception e)
            {
                Console.WriteLine(file);
                Console.WriteLine(e);
            }
        }
    }

    private static void ExtractChunkFile(string chr)
    {
        if (!ChunkFile.TryOpen(File.ReadAllBytes(chr), out var chunkFile))
            throw new Exception("Failed to open chunk file");

        chunkFile!.WriteXmlTo(Console.Out);
    }
}