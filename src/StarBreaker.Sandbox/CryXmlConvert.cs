namespace StarBreaker.Sandbox;

public static class CryXmlConvert
{
    public static void Run()
    {
        var entries = Directory.GetFiles(@"D:\StarCitizen\P4k\Data\Scripts", "*.xml", SearchOption.AllDirectories);
        Parallel.ForEach(entries, entry =>
        {
            if (entry.EndsWith("proper.xml"))
                return;

            if (CryXmlB.CryXml.TryOpen(File.OpenRead(entry), out var cryXml))
                cryXml.Save(Path.ChangeExtension(entry, "proper.xml"));
        });
    }
}