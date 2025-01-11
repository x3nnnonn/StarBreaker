using System.Text;
using System.Xml;
using System.Xml.Linq;
using StarBreaker.Chf;
using StarBreaker.Common;
using StarBreaker.DataCore;
using StarBreaker.P4k;

namespace StarBreaker.Sandbox;

public static class DataCoreSandbox
{
    public static void Run()
    {
        var p4k = P4kFile.FromFile(@"C:\Program Files\Roberts Space Industries\StarCitizen\PTU\Data.p4k");
        var dcbStream = p4k.OpenRead(@"Data\Game2.dcb");
        var dcb = new DataForge(dcbStream);

        Directory.CreateDirectory(@"D:\StarCitizen\DataCore\Sandbox");

        dcb.ExtractAll(@"D:\StarCitizen\DataCore\Sandbox");

        return;

        var megaMap = dcb.GetRecordsByFileName("*megamap.pu*").Values.Single();
        var tagDatabase = dcb.GetRecordsByFileName("*TagDatabase*").Values.Single();
        var broker = dcb.GetRecordsByFileName("*missionbroker.pu*").Values.Single();
        var unittest = dcb.GetRecordsByFileName("*unittesta*").Values.Single();
        //var someActorThing = dcb.DataCore.Database.GetRecord(new CigGuid("41d8fb15-72bb-446e-81df-eaecbc01e195"));
        var zeroggraph = dcb.GetRecordsByFileName("*playerzerogtraversalgraph*").Values.Single();

        dcb.GetFromRecord(zeroggraph).Save(@"D:\StarCitizen\DataCore\Sandbox\zeroggraph.xml");
        dcb.GetFromRecord(broker).Save(@"D:\StarCitizen\DataCore\Sandbox\broker.xml");
        dcb.GetFromRecord(unittest).Save(@"D:\StarCitizen\DataCore\Sandbox\unittesta.xml");
        //dcb.GetFromRecord(someActorThing).Save(@"D:\StarCitizen\DataCore\Sandbox\someActorThing.xml");
        dcb.GetFromRecord(tagDatabase).Save(@"D:\StarCitizen\DataCore\Sandbox\tagdatabase.xml");
        dcb.GetFromRecord(megaMap).Save(@"D:\StarCitizen\DataCore\Sandbox\megamap.xml");
        //
    }
}