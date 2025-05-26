using System.Globalization;
using Humanizer;
using StarBreaker.P4k;
using StarBreaker.Screens;

namespace StarBreaker.Extensions;

public static class ZipNodeExtensions
{
    public static string GetSize(this IP4kNode x)
    {
        return x switch
        {
            P4kFileNode file => ((long?)file.P4KEntry?.UncompressedSize)?.Bytes().ToString() ?? "",
            P4kSocPakFileNode socPak => ((long?)socPak.P4KEntry?.UncompressedSize)?.Bytes().ToString() ?? "",
            P4kSocPakChildFileNode socPakFile => ((long?)socPakFile.P4KEntry?.UncompressedSize)?.Bytes().ToString() ?? "",
            _ => ""
        };
    }

    public static string GetDate(this IP4kNode x)
    {
        return x switch
        {
            P4kFileNode file => file.P4KEntry?.LastModified.ToString("s", CultureInfo.InvariantCulture) ?? "",
            P4kSocPakFileNode socPak => socPak.P4KEntry?.LastModified.ToString("s", CultureInfo.InvariantCulture) ?? "",
            P4kSocPakChildFileNode socPakFile => socPakFile.P4KEntry?.LastModified.ToString("s", CultureInfo.InvariantCulture) ?? "",
            _ => ""
        };
    }

    public static string GetName(this IP4kNode x)
    {
        return x switch
        {
            P4kFileNode file => file.P4KEntry.Name.Split('\\').Last(),
            P4kDirectoryNode dir => dir.Name,
            FilteredP4kDirectoryNode filteredDir => filteredDir.Name,
            P4kSocPakFileNode socPak => Path.GetFileName(socPak.P4KEntry.Name),
            P4kSocPakDirectoryNode socPakDir => socPakDir.Name,
            P4kSocPakChildFileNode socPakFile => socPakFile.Name,
            _ => "",
        };
    }

    public static ICollection<IP4kNode> GetChildren(this IP4kNode x)
    {
        return x switch
        {
            P4kDirectoryNode dir => dir.Children.Values,
            FilteredP4kDirectoryNode filteredDir => filteredDir.FilteredChildren,
            P4kSocPakFileNode socPak => socPak.Children.Values,
            P4kSocPakDirectoryNode socPakDir => socPakDir.Children.Values,
            _ => Array.Empty<IP4kNode>()
        };
    }

    public static ulong SizeOrZero(this IP4kNode x)
    {
        return x switch
        {
            P4kFileNode file => file.P4KEntry?.UncompressedSize ?? 0,
            P4kSocPakFileNode socPak => socPak.P4KEntry?.UncompressedSize ?? 0,
            P4kSocPakChildFileNode socPakFile => socPakFile.P4KEntry?.UncompressedSize ?? 0,
            _ => 0
        };
    }
}