using System.Runtime.CompilerServices;
using StarBreaker.Common;
using StarBreaker.DataCore;
using StarBreaker.DataCoreGenerated;
using StarBreaker.P4k;

namespace StarBreaker.Sandbox;

public static class DataCoreSandbox
{
    public static void Run()
    {
        ExtractGenerated();
        //ExtractProblematic();
        //ExtractAll();
        //WriteJson();
    }

    private static void ExtractAll()
    {
        var timer = new TimeLogger();
        var dcb = DataForge.FromDcbXml(@"D:\StarCitizen\P4k\Data\Game2.dcb");
        timer.LogReset("Loaded DataForge");
        Directory.CreateDirectory(@"D:\StarCitizen\DataCore\Sandbox");
#if DEBUG
        dcb.ExtractAll(@"D:\StarCitizen\DataCore\Sandbox");
#else
        dcb.ExtractAllParallel(@"D:\StarCitizen\DataCore\Sandbox");
#endif
        timer.LogReset("Extracted all records.");
    }

    private static void ExtractProblematic()
    {
        var timer = new TimeLogger();

        var dcb = DataForge.FromDcbXml(@"D:\StarCitizen\P4k\Data\Game2.dcb");
        timer.LogReset("Loaded DataForge");

        var yy = dcb.DataCore.Database.MainRecords
            .AsParallel()
            .Select(x => dcb.GetFromRecord(x)).ToArray();

        timer.LogReset("Extracted all records.");

        return;
        Directory.CreateDirectory(@"D:\StarCitizen\DataCore\Sandbox");

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

    private static void ExtractGenerated()
    {
        var timer = new TimeLogger();
        var p4k = new P4kFileSystem(P4kFile.FromFile(@"C:\Program Files\Roberts Space Industries\StarCitizen\PTU\Data.p4k"));
        var dcbStream = p4k.OpenRead(@"Data\Game2.dcb");

        var df = new DataForge<IDataCoreReadable>(new DataCoreBinaryGenerated(new DataCoreDatabase(dcbStream)));
        
        timer.LogReset("Loaded DataForge");

        var megaMap = df.GetRecordsByFileName("*megamap.pu*").Values.Single();
        var tagDatabase = df.GetRecordsByFileName("*TagDatabase*").Values.Single();
        var broker = df.GetRecordsByFileName("*missionbroker.pu*").Values.Single();
        var unittest = df.GetRecordsByFileName("*unittesta*").Values.Single();
        var zeroggraph = df.GetRecordsByFileName("*playerzerogtraversalgraph*").Values.Single();
        var another = df.DataCore.Database.GetRecord(new CigGuid("04cd25f7-e0c6-4564-95ae-ecfc998e285f"));
        var bruh = df.GetFromRecord(tagDatabase);

        var yy = df.DataCore.Database.MainRecords
            .Select(x => df.GetFromRecord(x)).ToList();
        timer.LogReset("Extracted all records.");

        var zeroGTraversalGraphs = yy.OfType<ZeroGTraversalGraph>().ToList();

        Console.WriteLine();
    }

    private static void WriteJson()
    {
        var timer = new TimeLogger();
        var dcb = DataForge.FromDcbJson(@"D:\StarCitizen\P4k\Data\Game2.dcb");
        timer.LogReset("Loaded DataForge");
        Directory.CreateDirectory(@"D:\StarCitizen\DataCore\SandboxJson");
#if DEBUG
        dcb.ExtractAll(@"D:\StarCitizen\DataCore\SandboxJson");
#else
        dcb.ExtractAllParallel(@"D:\StarCitizen\DataCore\SandboxJson");
#endif
        timer.LogReset("Extracted all records.");
    }
}