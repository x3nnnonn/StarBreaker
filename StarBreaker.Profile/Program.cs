using System.Diagnostics;
using StarBreaker.CryChunkFile;
using StarBreaker.CryXmlB;
using StarBreaker.P4k;

namespace StarBreaker.Profile;

public static class Program
{    private const uint CrChMagic = 0x68437243; //"CrCh"u8
    private const uint IvoMagic = 0x6F766923;
    public static void Main(string[] args)
    {
        const string depot = @"D:\out";
        const string dest = @"D:\xml-parsed";
        const string p4k = @"C:\Program Files\Roberts Space Industries\StarCitizen\LIVE\Data.p4k";
        const string chr1 = @"data\\CRUS_Starlifter_Front_LandingGear_CHR.chr";
        const string chr2 = @"data\\ESPR_Talon_Glass_CHR.chr";
        const string chr = @"data\\ARGO_SRV_MainTractorBeamArm_CHR.chr";
        // string[] exts =
        // [
        //     "cga",
        //     "cgam",
        //     "cgf",
        //     "cgfm",
        //     "cigvoxel",
        //     "cigvoxelheader",
        //     "dst",
        //     "aim",
        //     "caf",
        //     "chr",
        //     "dba",
        //     "img",
        //     "skin",
        //     "skinm",
        // ];
        // using var crch = new StreamWriter(Path.Combine(depot, "crch.txt"));
        // using var ivo = new StreamWriter(Path.Combine(depot, "ivo.txt"));
        //
        // foreach (var ext in exts)
        // {
        //     foreach (var file in Directory.EnumerateFiles(depot, $"*.{ext}", SearchOption.AllDirectories))
        //     {
        //         using var fs = new BinaryReader(new FileStream(file, FileMode.Open, FileAccess.Read));
        //         var sig = fs.ReadUInt32();
        //         var destStream = sig switch
        //         {
        //             CrChMagic => crch,
        //             IvoMagic => ivo,
        //             _ => throw new Exception("Invalid signature")
        //         };
        //         
        //         fs.BaseStream.Seek(0, SeekOrigin.Begin);
        //         var buffer = new byte[128];
        //         fs.Read(buffer);
        //         destStream.WriteLine(BitConverter.ToString(buffer));
        //     }
        // }
        //
        var sw1 = Stopwatch.StartNew();

        if (!ChunkFile.TryOpen(File.ReadAllBytes(chr), out var chunkFile))
            throw new Exception("Failed to open chunk file");
        
        chunkFile!.WriteXmlTo(Console.Out);
        
        sw1.Stop();
        Console.WriteLine($"ChunkFile: {sw1.ElapsedMilliseconds}ms");
    }
}