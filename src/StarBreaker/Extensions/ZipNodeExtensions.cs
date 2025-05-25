using System.Globalization;
using Humanizer;
using StarBreaker.P4k;
using StarBreaker.Screens;
using StarBreaker.SocPak;

namespace StarBreaker.Extensions;

// Container node for SOCPAK files that can be expanded
public class SocPakContainerNode : IP4kNode, IDisposable
{
    public IP4kNode Parent { get; }
    public P4kFileNode P4kFileNode { get; }
    public SocPakFile? SocPakFile { get; private set; }
    public bool IsLoaded { get; private set; }
    public Exception? LoadError { get; private set; }
    
    private readonly Func<P4kFileNode, Stream>? _streamProvider;
    private string? _tempFile;

    public SocPakContainerNode(P4kFileNode p4kFileNode, Func<P4kFileNode, Stream>? streamProvider = null)
    {
        P4kFileNode = p4kFileNode;
        Parent = p4kFileNode.Parent;
        _streamProvider = streamProvider;
    }

    public void LoadSocPak()
    {
        if (IsLoaded || LoadError != null)
            return;
            
        try
        {
            if (_streamProvider == null)
            {
                LoadError = new InvalidOperationException("No stream provider available");
                return;
            }
                
            using var socPakStream = _streamProvider(P4kFileNode);
            
            // Save the stream to a temporary file since SocPakFile needs a file path
            _tempFile = Path.GetTempFileName();
            using (var fileStream = File.Create(_tempFile))
            {
                socPakStream.CopyTo(fileStream);
            }

            SocPakFile = SocPakFile.FromFile(_tempFile);
            IsLoaded = true;
            LoadError = null;
        }
        catch (Exception ex)
        {
            LoadError = ex;
            IsLoaded = false;
            CleanupTempFile();
        }
    }

    private void CleanupTempFile()
    {
        if (_tempFile != null && File.Exists(_tempFile))
        {
            try
            {
                File.Delete(_tempFile);
            }
            catch
            {
                // Ignore cleanup errors
            }
            _tempFile = null;
        }
    }

    public void Dispose()
    {
        SocPakFile?.Dispose();
        CleanupTempFile();
    }

    public void LoadSocPak(Stream socPakStream)
    {
        try
        {
            // Save the stream to a temporary file since SocPakFile needs a file path
            _tempFile = Path.GetTempFileName();
            using (var fileStream = File.Create(_tempFile))
            {
                socPakStream.CopyTo(fileStream);
            }

            SocPakFile = SocPakFile.FromFile(_tempFile);
            IsLoaded = true;
            LoadError = null;
        }
        catch (Exception ex)
        {
            LoadError = ex;
            IsLoaded = false;
            CleanupTempFile();
        }
    }
}

// Adapter to make SOCPAK nodes work with P4K interface
public class SocPakDirectoryAdapter : IP4kNode
{
    public IP4kNode Parent { get; }
    public SocPakDirectoryNode SocPakNode { get; }
    
    public SocPakDirectoryAdapter(SocPakDirectoryNode socPakNode, IP4kNode parent)
    {
        SocPakNode = socPakNode;
        Parent = parent;
    }
}

public class SocPakFileAdapter : IP4kNode
{
    public IP4kNode Parent { get; }
    public SocPakFileNode SocPakNode { get; }
    
    public SocPakFileAdapter(SocPakFileNode socPakNode, IP4kNode parent)
    {
        SocPakNode = socPakNode;
        Parent = parent;
    }
}

// Error node to show when SOCPAK loading fails
public class SocPakErrorNode : IP4kNode
{
    public IP4kNode Parent { get; }
    public Exception Error { get; }
    
    public SocPakErrorNode(Exception error, IP4kNode parent)
    {
        Error = error;
        Parent = parent;
    }
}

public static class ZipNodeExtensions
{
    public static string GetSize(this IP4kNode x)
    {
        return x switch
        {
            P4kFileNode file => ((long?)file.ZipEntry?.UncompressedSize)?.Bytes().ToString() ?? "",
            SocPakContainerNode container => ((long?)container.P4kFileNode.ZipEntry?.UncompressedSize)?.Bytes().ToString() ?? "",
            SocPakFileAdapter socFile => socFile.SocPakNode.Size.Bytes().ToString(),
            SocPakErrorNode => "",
            _ => ""
        };
    }

    public static string GetDate(this IP4kNode x)
    {
        return x switch
        {
            P4kFileNode file => file.ZipEntry?.LastModified.ToString("s", CultureInfo.InvariantCulture) ?? "",
            SocPakContainerNode container => container.P4kFileNode.ZipEntry?.LastModified.ToString("s", CultureInfo.InvariantCulture) ?? "",
            SocPakFileAdapter socFile => socFile.SocPakNode.LastModified.ToString("s", CultureInfo.InvariantCulture),
            SocPakErrorNode => "",
            _ => ""
        };
    }

    public static string GetName(this IP4kNode x)
    {
        return x switch
        {
            P4kFileNode file => file.ZipEntry.Name.Split('\\').Last(),
            P4kDirectoryNode dir => dir.Name,
            FilteredP4kDirectoryNode filteredDir => filteredDir.Name,
            SocPakContainerNode container => container.P4kFileNode.ZipEntry.Name.Split('\\').Last(),
            SocPakDirectoryAdapter socDir => socDir.SocPakNode.Name,
            SocPakFileAdapter socFile => socFile.SocPakNode.Name,
            SocPakErrorNode error => $"[Error] {error.Error.Message}",
            _ => "",
        };
    }

    public static ICollection<IP4kNode> GetChildren(this IP4kNode x)
    {
        return x switch
        {
            P4kDirectoryNode dir => dir.Children.Values,
            FilteredP4kDirectoryNode filteredDir => filteredDir.FilteredChildren,
            SocPakContainerNode container => GetSocPakContainerChildren(container),
            SocPakDirectoryAdapter socDir => ConvertSocPakChildren(socDir.SocPakNode, socDir),
            _ => Array.Empty<IP4kNode>()
        };
    }

    private static ICollection<IP4kNode> GetSocPakContainerChildren(SocPakContainerNode container)
    {
        // Lazy load the SOCPAK file when first accessed
        if (!container.IsLoaded && container.LoadError == null)
        {
            container.LoadSocPak();
        }
        
        if (!container.IsLoaded || container.SocPakFile == null)
        {
            // Return an error node if loading failed
            if (container.LoadError != null)
            {
                return new List<IP4kNode> { new SocPakErrorNode(container.LoadError, container) };
            }
            return Array.Empty<IP4kNode>();
        }

        return ConvertSocPakChildren(container.SocPakFile.Root, container);
    }

    private static ICollection<IP4kNode> ConvertSocPakChildren(SocPakDirectoryNode socPakDir, IP4kNode parent)
    {
        var result = new List<IP4kNode>();
        
        foreach (var child in socPakDir.GetAllNodes())
        {
            if (child is SocPakDirectoryNode childDir)
            {
                result.Add(new SocPakDirectoryAdapter(childDir, parent));
            }
            else if (child is SocPakFileNode childFile)
            {
                result.Add(new SocPakFileAdapter(childFile, parent));
            }
        }
        
        return result;
    }

    public static ulong SizeOrZero(this IP4kNode x)
    {
        return x switch
        {
            P4kFileNode file => file.ZipEntry?.UncompressedSize ?? 0,
            SocPakContainerNode container => container.P4kFileNode.ZipEntry?.UncompressedSize ?? 0,
            SocPakFileAdapter socFile => (ulong)socFile.SocPakNode.Size,
            SocPakErrorNode => 0,
            _ => 0
        };
    }

    public static bool IsSocPakFile(this IP4kNode node)
    {
        if (node is not P4kFileNode fileNode)
            return false;

        return fileNode.ZipEntry.Name.EndsWith(".socpak", StringComparison.OrdinalIgnoreCase);
    }
}