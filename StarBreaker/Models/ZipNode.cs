using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Humanizer;
using ReactiveUI;
using StarBreaker.P4k;

namespace StarBreaker.Models;

public class ZipNode : ReactiveObject
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
    public string DateModifiedUi => ZipEntry?.LastModified.ToString("s", CultureInfo.InvariantCulture) ?? "";
    public string CompressionMethodUi => ZipEntry?.CompressionMethod.ToString() ?? "";
    public string EncryptedUi => ZipEntry?.IsCrypted.ToString() ?? "";
    
    private bool _isChecked;

    public bool IsChecked
    {
        get => _isChecked;
        set => this.RaiseAndSetIfChanged(ref _isChecked, value);
    }
    
    public ZipNode(IEnumerable<ZipEntry> zipEntries)
    {
        Name = "";
        var root = new ZipNode("");
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
        }
        
        Children = root.Children;
    }
}