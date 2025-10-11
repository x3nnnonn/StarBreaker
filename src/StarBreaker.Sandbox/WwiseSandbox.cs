using StarBreaker.Wwise.Bnk;

namespace StarBreaker.Sandbox;

public static class WwiseSandbox
{
    public static void Run()
    {
        
        var allBnks = Directory.GetFiles(@"D:\StarCitizen\P4kbnk\Data\Sounds\wwise", "*Default.bnk", SearchOption.AllDirectories);
        
        //sort by largest to smallest
        allBnks = allBnks.OrderByDescending(x => new FileInfo(x).Length).ToArray();
        

        var sectionTypeCounts = new Dictionary<BnkSectionType, int>();
        foreach(var sectionType in Enum.GetValues<BnkSectionType>())
        {
            sectionTypeCounts[sectionType] = 0;
        }
        
        foreach (var bnk in allBnks)
        {
            var file = BnkFile.Open(File.OpenRead(bnk));
            foreach (var type in file.SectionData.Keys)
            {
                sectionTypeCounts[type]++;
            }
        }
        
        foreach (var kv in sectionTypeCounts)
        {
            Console.WriteLine($"{kv.Key}: {kv.Value}");
        }
    }
}