using System.Text;
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

        using var writer = new StreamWriter(@"D:\tagdatabase.xml");
        //dcb.ExtractSingleRecord(writer, tagDatabase);

        dcb.ExtractAll(@"D:\\DataCore", progress: new Progress<double>(x => { Console.WriteLine($"Progress: {x:P}"); }));
    }
}