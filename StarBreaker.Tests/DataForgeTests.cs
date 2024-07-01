using StarBreaker.Forge;

namespace StarBreaker.Tests;

public class Tests
{
    private string _target;
    
    [SetUp]
    public void Setup()
    {
        _target = File.ReadAllText(Path.Combine("TestData", "TagDatabase.TagDatabase.xml"));
    }

    [Test]
    public void Test1()
    {
        var forge = new DataForge(Path.Combine("TestData", "Game.dcb"));
        
        var stringwriter = new StringWriter();
        forge.X(@"libs/foundry/records/tagdatabase/tagdatabase.tagdatabase.xml", stringwriter);
        
        var s = stringwriter.ToString();
        Assert.That(s, Is.EqualTo(_target));
    }
}