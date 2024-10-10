using StarBreaker.Forge;

namespace StarBreaker.Tests;

public class Tests
{
    private string _target;
    
    [SetUp]
    public void Setup()
    {
        _target = File.ReadAllText(@"D:\StarCitizenExport\Data\Libs\Foundry\Records\TagDatabase\TagDatabase.TagDatabase.xml");
    }

    [Test]
    public void TestTagDatabase()
    {
        var forge = new DataForge(@"D:\out\Data\Game.dcb");
        var tagdatabase = forge.GetRecordsByFileName("*TagDatabase*");
        
        var writer = new StringWriter();
        forge.ExtractSingleRecord(writer, tagdatabase.Values.Single());
        
        var s = writer.ToString();
        Assert.That(s, Is.EqualTo(_target));
    }
    
    [Test]
    public void Enums()
    {
        var forge = new DataForge(@"D:\out\Data\Game.dcb");
        var enums = forge.ExportEnums();
        
        var writer = new StringWriter();
    }
}