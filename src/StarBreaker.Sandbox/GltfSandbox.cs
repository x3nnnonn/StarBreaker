using System.Text;
using StarBreaker.Common;
using StarBreaker.CryChunkFile;
using StarBreaker.P4k;

namespace StarBreaker.Sandbox;

public static class GltfSandbox
{
    public static void Run()
    {
        var data = File.ReadAllBytes(@"D:\StarCitizen\P4k\Data\Objects\default\teapot.cgf");
        var datam =  File.ReadAllBytes(@"D:\StarCitizen\P4k\Data\Objects\default\teapot.cgfm");
        
        if (!IvoFile.TryRead(data, out var ivo) || !IvoFile.TryRead(datam, out var ivom))
        {
            Console.WriteLine("Failed to open chunk file");
            return;
        }
        
        
        
        Console.WriteLine($"Chunk count: {ivo.Chunks.Length}");
    }
}