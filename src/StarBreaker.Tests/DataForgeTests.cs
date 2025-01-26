using System.Xml.Linq;
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
        var df = new DataForge<XElement>(
            new DataCoreBinaryXml(
                new DataCoreDatabase(
                    File.OpenRead(@"D:\StarCitizen\P4k\Data\Game.dcb")
                )
            )
        );
        var tagdatabase = df.GetRecordsByFileName("*TagDatabase*");

        var writer = new StringWriter();
        df.GetFromRecord(tagdatabase.Values.Single()).Save(writer);

        var expected = await File.ReadAllTextAsync("TagDatabase.TagDatabase.xml");
        var actual = writer.ToString();
        await Assert.That(actual).IsEqualTo(expected);
    }

    [Test]
    public async Task Enums()
    {
        var dcb = new DataForge<XElement>(
            new DataCoreBinaryXml(
                new DataCoreDatabase(
                    File.OpenRead(@"D:\StarCitizen\P4k\Data\Game.dcb")
                )
            )
        );
        var enums = dcb.ExportEnums();

        await Assert.That(enums).IsNotEmpty();
        await Assert.That(enums.All(e => e.Value.Length > 0)).IsTrue();
    }
}