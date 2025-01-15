using StarBreaker.Common;
using StarBreaker.DataCore;

namespace StarBreaker.Sandbox;

public static class DataCoreSandbox
{
    public static void Run()
    {
        ExtractAll();
    }

    private static void ExtractAll()
    {
        var timer = new TimeLogger();
        var dcb = new DataForge(new MemoryStream(File.ReadAllBytes(@"D:\StarCitizen\P4k\Data\Game2.dcb")));
        timer.LogReset("Loaded DataForge");
        Directory.CreateDirectory(@"D:\StarCitizen\DataCore\Sandbox");
        dcb.ExtractAll(@"D:\StarCitizen\DataCore\Sandbox");
        timer.LogReset("Extracted all records.");
    }

    private static void ExtractProblematic()
    {
        var timer = new TimeLogger();

        var dcb = new DataForge(new MemoryStream(File.ReadAllBytes(@"D:\StarCitizen\P4k\Data\Game2.dcb")));
        timer.LogReset("Loaded DataForge");
        Directory.CreateDirectory(@"D:\StarCitizen\DataCore\Sandbox");

        var megaMap = dcb.GetRecordsByFileName("*megamap.pu*").Values.Single();
        var tagDatabase = dcb.GetRecordsByFileName("*TagDatabase*").Values.Single();
        var broker = dcb.GetRecordsByFileName("*missionbroker.pu*").Values.Single();
        var unittest = dcb.GetRecordsByFileName("*unittesta*").Values.Single();
        var zeroggraph = dcb.GetRecordsByFileName("*playerzerogtraversalgraph*").Values.Single();

        dcb.GetFromRecord(zeroggraph).Save(@"D:\StarCitizen\DataCore\Sandbox\zeroggraph.xml");
        dcb.GetFromRecord(broker).Save(@"D:\StarCitizen\DataCore\Sandbox\broker.xml");
        dcb.GetFromRecord(unittest).Save(@"D:\StarCitizen\DataCore\Sandbox\unittesta.xml");
        dcb.GetFromRecord(tagDatabase).Save(@"D:\StarCitizen\DataCore\Sandbox\tagdatabase.xml");
        dcb.GetFromRecord(megaMap).Save(@"D:\StarCitizen\DataCore\Sandbox\megamap.xml");
    }
}