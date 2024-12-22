using System.Globalization;
using Humanizer;
using StarBreaker.P4k;

namespace StarBreaker.Extensions;

public static class ZipNodeExtensions
{
    public static string GetSize(this ZipNode x) => ((long?)x.ZipEntry?.UncompressedSize)?.Bytes().ToString() ?? "";
    public static string GetModifiedDate(this ZipNode x) => x.ZipEntry?.LastModified.ToString("yyyy-mm-dd", CultureInfo.InvariantCulture) ?? "";
    public static string GetName(this ZipNode x) => x.Name ?? x.ZipEntry!.Name.Split('\\').Last();
}