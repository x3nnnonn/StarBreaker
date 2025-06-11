using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using StarBreaker.Common;
using StarBreaker.DataCore;
using StarBreaker.P4k;

namespace StarBreaker.Sandbox;

public static class DataCoreSandbox
{
    public static void Run()
    {
        TryBruteForceGuid();
        //ExtractProblematic();
        //ExtractAll();
        // ExtractJson();
        // ExtractXml();
    }

    private static void TryBruteForceGuid()
    {
        var timer = new TimeLogger();
        var dcb = DataForge.FromDcbPathXml(@"D:\StarCitizen\P4k\Data\Game2.dcb");
        timer.LogReset("Loaded DataForge");

        const uint target = 1323318587;

        var recordGuids = dcb.DataCore.Database.RecordDefinitions
            .Select(s => s.Id).ToArray();

        var needle = recordGuids.FirstOrDefault(g =>
        {
            var crcbytes = Crc32c.FromSpan(MemoryMarshal.Cast<CigGuid, byte>([g]));

            return crcbytes == target;
        });

        timer.LogReset("Brute force search completed.");

        Console.WriteLine(needle == default ? "No matching GUID found." : $"Found matching GUID: {needle}");
    }

    private static void ExtractProblematic()
    {
        var timer = new TimeLogger();

        var dcb = DataForge.FromDcbPathXml(@"D:\StarCitizen\P4k\Data\Game2.dcb");
        timer.LogReset("Loaded DataForge");

        var megaMap = dcb.GetRecordsByFileName("*megamap.pu*").Values.Single();
        var tagDatabase = dcb.GetRecordsByFileName("*TagDatabase*").Values.Single();
        var broker = dcb.GetRecordsByFileName("*missionbroker.pu*").Values.Single();
        var unittest = dcb.GetRecordsByFileName("*unittesta*").Values.Single();
        var zeroggraph = dcb.GetRecordsByFileName("*playerzerogtraversalgraph*").Values.Single();
        var another = dcb.DataCore.Database.GetRecord(new CigGuid("04cd25f7-e0c6-4564-95ae-ecfc998e285f"));

        //dcb.GetFromRecord(another).Save(@"D:\StarCitizen\DataCore\Sandbox\another.xml");
        // dcb.GetFromRecord(zeroggraph).Save(@"D:\StarCitizen\DataCore\Sandbox\zeroggraph.xml");
        // dcb.GetFromRecord(broker).Save(@"D:\StarCitizen\DataCore\Sandbox\broker.xml");
        //dcb.GetFromRecord(unittest).Save(@"D:\StarCitizen\DataCore\Sandbox\unittesta.xml");
        var db = dcb.GetFromRecord(tagDatabase);

        Console.WriteLine(db);
        //.Save(@"D:\StarCitizen\DataCore\Sandbox\tagdatabase.xml");
        // dcb.GetFromRecord(megaMap).Save(@"D:\StarCitizen\DataCore\Sandbox\megamap.xml");
    }

    private static void ExtractXml()
    {
        var timer = new TimeLogger();
        var dcb = DataForge.FromDcbPathXml(@"D:\StarCitizen\P4k\Data\Game2.dcb");
        timer.LogReset("Loaded DataForge");
        Directory.CreateDirectory(@"D:\StarCitizen\DataCoreSandbox\Xml");
#if DEBUG
        dcb.ExtractAll(@"D:\StarCitizen\DataCore\Sandbox");
#else
        dcb.ExtractAllParallel(@"D:\StarCitizen\DataCoreSandbox\Xml");
#endif
        timer.LogReset("Extracted all records.");
    }

    private static void ExtractJson()
    {
        var timer = new TimeLogger();
        var dcb = DataForge.FromDcbPathJson(@"D:\StarCitizen\P4k\Data\Game2.dcb");
        timer.LogReset("Loaded DataForge");
        Directory.CreateDirectory(@"D:\StarCitizen\DataCoreSandbox\Json");
#if DEBUG
        dcb.ExtractAll(@"D:\StarCitizen\DataCore\SandboxJson");
#else
        dcb.ExtractAllParallel(@"D:\StarCitizen\DataCoreSandbox\Json");
#endif
        timer.LogReset("Extracted all records.");
    }
}