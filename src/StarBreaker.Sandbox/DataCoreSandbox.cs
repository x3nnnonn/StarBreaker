using System.Runtime.CompilerServices;
using System.Xml.Linq;
using StarBreaker.Common;
using StarBreaker.DataCore;
using StarBreaker.DataCoreGenerated;

namespace StarBreaker.Sandbox;

public static class DataCoreSandbox
{
    public static void Run()
    {
        //GenerateTypes();
        //ExtractGenerated();

        //ExtractUnp4k();
        //ExtractProblematic();
        // ExtractAll();
        WriteJson();
    }

    private static void GenerateTypes()
    {
        var timer = new TimeLogger();
        var dcr = new DataCoreCodeGenerator(new DataCoreDatabase(new MemoryStream(File.ReadAllBytes(@"D:\StarCitizen\P4k\Data\Game2.dcb"))));
        timer.LogReset("Loaded DataForge");

        dcr.Generate(@"C:\Development\StarCitizen\StarBreaker\src\StarBreaker.DataCore.Generated\Generated");

        timer.LogReset("Generate");
    }

    private static void ExtractUnp4k()
    {
        var timer = new TimeLogger();
        var dcb = new DataForge<XElement>(
            new DataCoreBinaryXml(
                new DataCoreDatabase(
                    new MemoryStream(File.ReadAllBytes(@"D:\StarCitizen\P4k\Data\Game2.dcb"))
                )
            )
        );
        timer.LogReset("Loaded DataForge");
        dcb.ExtractUnp4k(@"D:\unp4k\Unpak.xml");
        timer.LogReset("Extracted all records.");
    }

    private static void ExtractAll()
    {
        var timer = new TimeLogger();
        var dcb = new DataForge<XElement>(
            new DataCoreBinaryXml(
                new DataCoreDatabase(
                    new MemoryStream(File.ReadAllBytes(@"D:\StarCitizen\P4k\Data\Game2.dcb"))
                )
            )
        );
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

        var dcb = new DataForge<XElement>(
            new DataCoreBinaryXml(
                new DataCoreDatabase(
                    new MemoryStream(File.ReadAllBytes(@"D:\StarCitizen\P4k\Data\Game2.dcb"))
                )
            )
        );
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

        var dcb = new DataForge<XElement>(new DataCoreBinaryXml(new DataCoreDatabase(new MemoryStream(File.ReadAllBytes(@"D:\StarCitizen\P4k\Data\Game2.dcb")))));
        timer.LogReset("Loaded DataForge");


        var megaMap = dcb.GetRecordsByFileName("*megamap.pu*").Values.Single();
        var tagDatabase = dcb.GetRecordsByFileName("*TagDatabase*").Values.Single();
        var broker = dcb.GetRecordsByFileName("*missionbroker.pu*").Values.Single();
        var unittest = dcb.GetRecordsByFileName("*unittesta*").Values.Single();
        var zeroggraph = dcb.GetRecordsByFileName("*playerzerogtraversalgraph*").Values.Single();
        var another = dcb.DataCore.Database.GetRecord(new CigGuid("04cd25f7-e0c6-4564-95ae-ecfc998e285f"));

        var yy = dcb.DataCore.Database.MainRecords
            .Select(x =>
            {
                var y = dcb.DataCore.Database.GetRecord(x);
                return TypeMap.ReadFromRecord(dcb.DataCore.Database, y.StructIndex, y.InstanceIndex);
            }).ToList();
        timer.LogReset("Extracted all records.");

        var gladius = yy.OfType<ZeroGTraversalGraph>().ToList();

        //stupid idiots at CIG decided to have enumDefinition options be string id 2,
        // but the enum values in the data map to id 1. wonderful. we can't do a (fast) dumb cast properly unless we want it to be jank.
        // if we have to enum.Parse I'll be sad :(

        //actual
        var b = Unsafe.BitCast<uint, DataCoreStringId>(2687);
        var d = Unsafe.BitCast<uint, DataCoreStringId>(4858636);

        //expected 
        var e = Unsafe.BitCast<uint, DataCoreStringId2>(7321);
        var r = Unsafe.BitCast<uint, DataCoreStringId2>(7085);

        Console.WriteLine();
    }

    private static void WriteJson()
    {
        var timer = new TimeLogger();
        var dcb = new DataForge<string>(new DataCoreBinaryJson(new DataCoreDatabase(File.OpenRead(@"D:\StarCitizen\P4k\Data\Game2.dcb"))));
        return;
        timer.LogReset("Loaded DataForge");
        Directory.CreateDirectory(@"D:\StarCitizen\DataCore\SandboxJson");
#if DEBUG
        dcb.ExtractAll(@"D:\StarCitizen\DataCore\SandboxJson");
#else
        dcb.ExtractAllParallel(@"C:\StarCitizen\DataCore\SandboxJson");
#endif
        timer.LogReset("Extracted all records.");
    }
}