using StarBreaker.Forge;

namespace StarBreaker.Tests;

public class Tests
{
    /// <summary>
    /// This test is failing. I use it to figure out how to correctly construct the xml file.
    /// </summary>
    [Test]
    public void TestTagDatabase()
    {
        var forge = new DataForge(@"D:\out\Data\Game.dcb");
        var tagdatabase = forge.GetRecordsByFileName("*TagDatabase*");
        
        var writer = new StringWriter();
        forge.ExtractSingleRecord(writer, tagdatabase.Values.Single());
        
        var expected = File.ReadAllText("TagDatabase.TagDatabase.xml");
        var actual = writer.ToString();
        Assert.That(actual, Is.EqualTo(expected));
    }
    
    [Test]
    public void Enums()
    {
        var forge = new DataForge(@"D:\out\Data\Game.dcb");
        var enums = forge.ExportEnums();
        
        Assert.That(enums, Is.Not.Empty);
        Assert.That(enums.All(e => e.Value.Length > 0));
    }
}