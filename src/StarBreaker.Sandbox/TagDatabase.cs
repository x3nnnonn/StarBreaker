using System.Text;
using System.Xml;
using System.Xml.Linq;
using StarBreaker.Chf;
using StarBreaker.Common;
using StarBreaker.DataCore;
using StarBreaker.P4k;

namespace StarBreaker.Sandbox;

public static class TagDatabase
{
    public static void Run()
    {
        var p4k = P4kFile.FromFile(@"C:\Program Files\Roberts Space Industries\StarCitizen\4.0_PREVIEW\Data.p4k");
        var dcbStream = p4k.OpenRead(@"Data\Game2.dcb");
        var dcb = new DataForge(dcbStream);
        var tagDatabase = dcb.DataCore.GetRecordsByFileName("*TagDatabase*").Values.Single();
        var someActorThing = dcb.DataCore.Database.GetRecord(new CigGuid("001087eb-84db-4bf4-a912-f641894aa543"));
        var rec = dcb.DataCore.GetFromRecord(tagDatabase);
        rec.Save(@"D:\rec.xml");
    }
}