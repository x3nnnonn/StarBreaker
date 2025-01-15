using System.Diagnostics;
using StarBreaker.DataCore;
using StarBreaker.P4k;

namespace StarBreaker.Sandbox;

public static class DataCoreSandbox
{
    public static void Run()
    {
        var p4k = new P4kFileSystem(P4kFile.FromFile(@"C:\Program Files\Roberts Space Industries\StarCitizen\PTU\Data.p4k"));
        var dcbStream = p4k.OpenRead(@"Data\Game2.dcb");
        var dcb = new DataForge(dcbStream);

        Directory.CreateDirectory(@"D:\StarCitizen\DataCore\Sandbox");
        //
        // var megaMap = dcb.GetRecordsByFileName("*megamap.pu*").Values.Single();
        // var tagDatabase = dcb.GetRecordsByFileName("*TagDatabase*").Values.Single();
        // var broker = dcb.GetRecordsByFileName("*missionbroker.pu*").Values.Single();
        // var unittest = dcb.GetRecordsByFileName("*unittesta*").Values.Single();
        // var zeroggraph = dcb.GetRecordsByFileName("*playerzerogtraversalgraph*").Values.Single();
        //
        // dcb.GetFromRecord(zeroggraph).Save(@"D:\StarCitizen\DataCore\Sandbox\zeroggraph.xml");
        // dcb.GetFromRecord(broker).Save(@"D:\StarCitizen\DataCore\Sandbox\broker.xml");
        // dcb.GetFromRecord(unittest).Save(@"D:\StarCitizen\DataCore\Sandbox\unittesta.xml");
        // dcb.GetFromRecord(tagDatabase).Save(@"D:\StarCitizen\DataCore\Sandbox\tagdatabase.xml");
        // dcb.GetFromRecord(megaMap).Save(@"D:\StarCitizen\DataCore\Sandbox\megamap.xml");
        var before = Stopwatch.GetTimestamp();
        dcb.ExtractAll(@"D:\StarCitizen\DataCore\Sandbox");
        var diff = Stopwatch.GetElapsedTime(before);

        Console.WriteLine($"Extracted all records in {diff.TotalMilliseconds}ms");
    }
}