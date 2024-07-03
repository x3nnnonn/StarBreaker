using System.Globalization;
using System.Runtime.InteropServices;
using StarBreaker.Forge;

namespace StarBreaker.Tests;


public class CigGuidTests
{
    private const string GUID_STRING = "66ee5bfc-d90b-41bd-ad2e-e0a2b3efe359";
    private const string GUID_BYTES = "BD-41-0B-D9-FC-5B-EE-66-59-E3-EF-B3-A2-E0-2E-AD";
    
    [Test]
    public void TestParseCigGuid()
    {
        var actualBytes = GUID_BYTES.Split('-').Select(x => byte.Parse(x, NumberStyles.HexNumber)).ToArray();

        var directCig = MemoryMarshal.Read<CigGuid>(actualBytes);

        Assert.That(directCig.ToString(), Is.EqualTo(GUID_STRING));
    }
}