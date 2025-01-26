using System.IO.Enumeration;
using System.Text;

namespace StarBreaker.DataCore;

public static class DataCoreUtils
{
    public static readonly string[] KnownPaths = [@"Data\Game2.dcb", @"Data\Game.dcb"];
    public static bool IsDataCoreFile(string path)
    {
        return FileSystemName.MatchesSimpleExpression(@"Data\*.dcb", path);
    }

    internal static string ComputeRelativePath(ReadOnlySpan<char> filePath, ReadOnlySpan<char> contextFileName)
    {
        var slashes = contextFileName.Count('/');
        var sb = new StringBuilder("file://./");

        for (var i = 0; i < slashes; i++)
            sb.Append("../");

        sb.Append(filePath);
        return sb.ToString();
    }
}