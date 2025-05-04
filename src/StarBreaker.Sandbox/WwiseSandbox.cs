using StarBreaker.Wwise.Bnk;

namespace StarBreaker.Sandbox;

public static class WwiseSandbox
{
    public static void Run()
    {
        var bnk = BnkFile.Open(File.OpenRead(@"D:\StarCitizen\P4k\Data\Sounds\wwise\SSAM_CRUS_StarFighter.bnk"));
        bnk.ExtractWemFiles(@"D:\StarCitizen\Wems");
        Console.WriteLine(bnk);
    }
}