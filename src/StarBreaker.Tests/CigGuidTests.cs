using System.Runtime.InteropServices;
using StarBreaker.Common;

namespace StarBreaker.Tests;


public class CigGuidTests
{
    private const string GUID_STRING = "66ee5bfc-d90b-41bd-ad2e-e0a2b3efe359";
    private const string GUID_BYTES = "BD410BD9FC5BEE6659E3EFB3A2E02EAD";
    
    [Test]
    public async Task CigGuidFromBytes()
    {
        var actualBytes = Convert.FromHexString(GUID_BYTES);

        var directCig = MemoryMarshal.Read<CigGuid>(actualBytes);

        await Assert.That(directCig.ToString()).IsEqualTo(GUID_STRING);
    }

    [Test]
    public async Task CigGuidToString()
    {
        var cigguid = new CigGuid(GUID_STRING);
        var stringrepresentation = cigguid.ToString();
        await Assert.That(stringrepresentation).IsEqualTo(GUID_STRING);
    }
}