using System.Globalization;
using Humanizer;
using StarBreaker.P4k;

namespace StarBreaker.Extensions;

public static class ZipNodeExtensions
{
    public static string GetSizeText(this IP4kNode x)
    {
        return ((long)x.Size).Bytes().Humanize();
    }

    public static string GetDate(this IP4kNode x)
    {
        if (x is not P4kFileNode file)
            return "";

        return file.P4KEntry.LastModified.ToString("s", CultureInfo.InvariantCulture) ?? "";
    }

    public static ICollection<IP4kNode> GetChildren(this IP4kNode x)
    {
        return x switch
        {
            IP4kDirectoryNode dir => dir.Children.Values,
            _ => Array.Empty<IP4kNode>(),
        };
    }
}