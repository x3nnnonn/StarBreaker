using StarBreaker.Forge;

namespace StarBreaker.Tests;

public class Tests
{
    /// <summary>
    /// This test is failing. I use it to figure out how to correctly construct the xml file.
    /// </summary>
    [Test]
    public async Task TestTagDatabase()
    {
        var forge = new DataForge(@"D:\out\Data\Game.dcb");
        var tagdatabase = forge.GetRecordsByFileName("*TagDatabase*");
        
        var writer = new StringWriter();
        forge.ExtractSingleRecord(writer, tagdatabase.Values.Single());
        
        var expected = await File.ReadAllTextAsync("TagDatabase.TagDatabase.xml");
        var actual = writer.ToString();
        await Assert.That(actual).IsEqualTo(expected);
    }

    [Test]
    public async Task Enums()
    {
        var forge = new DataForge(@"D:\out\Data\Game.dcb");
        var enums = forge.ExportEnums();

        await Assert.That(enums).IsNotEmpty();
        await Assert.That(enums.All(e => e.Value.Length > 0)).IsTrue();
    }
}