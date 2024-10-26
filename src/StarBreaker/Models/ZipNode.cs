using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Humanizer;
using StarBreaker.P4k;
using StarBreaker.Screens;

namespace StarBreaker.Models;

public partial class ZipNode : ViewModelBase
{
    private static readonly Dictionary<string, ZipNode> EmptyList = new();

    public ZipEntry? ZipEntry { get; }
    public string Name { get; }
    public Dictionary<string, ZipNode> Children { get; }

    /// <summary>
    ///     Constructor for creating a file node
    /// </summary>
    /// <param name="entry"></param>
    public ZipNode(ZipEntry entry)
    {
        ZipEntry = entry;
        Name = entry.Name.Split('\\').Last();
        Children = EmptyList;
    }

    /// <summary>
    ///     Constructor for creating a directory node
    /// </summary>
    /// <param name="name"></param>
    public ZipNode(string name)
    {
        Name = name;
        Children = new Dictionary<string, ZipNode>();
    }

    public string SizeUi => ((long?)ZipEntry?.UncompressedSize)?.Bytes().ToString() ?? "";
    public string DateModifiedUi => ZipEntry?.LastModified.ToString("yyyy-mm-dd", CultureInfo.InvariantCulture) ?? "";
    public string CompressionMethodUi => ZipEntry?.CompressionMethod.ToString() ?? "";
    public string EncryptedUi => ZipEntry?.IsCrypted.ToString() ?? "";

    [ObservableProperty]
    private bool _isChecked;

    public ZipNode(ZipEntry[] zipEntries, IProgress<double>? progress = null)
    {
        progress?.Report(0);
        var report = Math.Max(1, zipEntries.Length / 100);
        Name = "";
        var root = new ZipNode("");
        int count = 0;
        foreach (var zipEntry in zipEntries)
        {
            var parts = zipEntry.Name.Split('\\');
            var current = root;

            for (var index = 0; index < parts.Length; index++)
            {
                var part = parts[index];

                // If this is the last part, we're at the file
                if (index == parts.Length - 1)
                {
                    current.Children[part] = new ZipNode(zipEntry);
                    break;
                }

                if (!current.Children.TryGetValue(part, out var value))
                {
                    value = new ZipNode(part);
                    current.Children[part] = value;
                }

                current = value;
            }
            count++;
            if (progress != null && count % report == 0)
            {
                progress.Report(count / (double)zipEntries.Count());
            }
        }
        
        Children = root.Children;
    }
}