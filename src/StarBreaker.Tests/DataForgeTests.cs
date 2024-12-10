using StarBreaker.DataCore;

namespace StarBreaker.Tests;

public class Tests
{
    /// <summary>
    /// This test is failing. I use it to figure out how to correctly construct the xml file.
    /// </summary>
    [Test]
    public async Task TestTagDatabase()
    {
        var dcb = new DataCoreBinary(File.OpenRead(@"D:\StarCitizen\p4k\Data\Game.dcb"));
        var tagdatabase = dcb.GetRecordsByFileName("*TagDatabase*");
        
        var writer = new StringWriter();
        dcb.ExtractSingleRecord(writer, tagdatabase.Values.Single());
        
        var expected = await File.ReadAllTextAsync("TagDatabase.TagDatabase.xml");
        var actual = writer.ToString();
        await Assert.That(actual).IsEqualTo(expected);
    }

    [Test]
    public async Task Enums()
    {
        var dcb = new DataCoreBinary(File.OpenRead(@"C:\Scratch\StarCitizen\p4k\Data\Game.dcb"));
        var enums = dcb.ExportEnums();

        await Assert.That(enums).IsNotEmpty();
        await Assert.That(enums.All(e => e.Value.Length > 0)).IsTrue();
    }
}