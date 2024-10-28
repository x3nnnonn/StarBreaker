using StarBreaker.Dds;

namespace StarBreaker.Sandbox;

public static class DdsUnsplit
{
    public static void Run()
    {
        var dds = DdsFile.FromFile(@"C:\Scratch\StarCitizen\p4k\Data\Textures\planets\global\stanton\stanton1\stanton1_clouds_global.dds");
    }
}