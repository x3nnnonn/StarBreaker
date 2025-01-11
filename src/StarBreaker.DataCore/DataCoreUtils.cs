using System.IO.Enumeration;

namespace StarBreaker.DataCore;

public static class DataCoreUtils
{
    public static readonly string[] KnownPaths = [@"Data\Game2.dcb", @"Data\Game.dcb"];
    public static bool IsDataCoreFile(string path)
    {
        return FileSystemName.MatchesSimpleExpression("Data\\*.dcb", path);
    }
}