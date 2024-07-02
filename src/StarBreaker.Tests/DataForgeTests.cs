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
        var forge = new DataForge(@"D:\StarCitizenExport\Data\Game.dcb");
        
        var stringwriter = new StringWriter();
        forge.X(@"libs/foundry/records/tagdatabase/tagdatabase.tagdatabase.xml", stringwriter);
        
        var s = stringwriter.ToString();
        Assert.That(s, Is.EqualTo(_target));
    }
}