using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Selection;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using StarBreaker.Common;
using StarBreaker.CryXmlB;
using StarBreaker.Dds;
using StarBreaker.Extensions;
using StarBreaker.P4k;
using StarBreaker.Services;
using StarBreaker.SocPak;
using System.IO;

namespace StarBreaker.Screens;

// Helper class for filtered directory display during search
public class FilteredP4kDirectoryNode : IP4kNode
{
    public IP4kNode Parent { get; }
    public string Name { get; }
    public ICollection<IP4kNode> FilteredChildren { get; }

    public FilteredP4kDirectoryNode(string name, IP4kNode parent, ICollection<IP4kNode> filteredChildren)
    {
        Name = name;
        Parent = parent;
        FilteredChildren = filteredChildren;
    }
}

public sealed partial class P4kTabViewModel : PageViewModelBase
{
    public override string Name => "P4k";
    public override string Icon => "ZipFolder";

    private readonly ILogger<P4kTabViewModel> _logger;
    private readonly IP4kService _p4KService;
    private readonly IPreviewService _previewService;

    [ObservableProperty] private HierarchicalTreeDataGridSource<IP4kNode> _source = null!;
    [ObservableProperty] private FilePreviewViewModel? _preview;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private double _extractionProgress;
    [ObservableProperty] private bool _isExtracting;
    [ObservableProperty] private bool _convertDdsToPng = true;

    private ICollection<IP4kNode> _allRootNodes;

    public P4kTabViewModel(IP4kService p4kService, ILogger<P4kTabViewModel> logger, IPreviewService previewService)
    {
        _p4KService = p4kService;
        _logger = logger;
        _previewService = previewService;
        
        InitializeTreeDataGrid();
        
        _allRootNodes = _p4KService.P4KFileSystem.Root.Children.Values;
        Source.Items = ProcessAndSortNodes(_allRootNodes);
        
        // Initialize commands explicitly
        ExtractSelectedNodeCommand = new RelayCommand(async () => await ExtractSelectedNode());
        
        _logger.LogInformation("P4kTabViewModel initialized");
    }
    
    public IRelayCommand ExtractSelectedNodeCommand { get; }
    
    private void InitializeTreeDataGrid()
    {
        Source = new HierarchicalTreeDataGridSource<IP4kNode>(Array.Empty<IP4kNode>())
        {
            Columns =
            {
                new HierarchicalExpanderColumn<IP4kNode>(
                    new TextColumn<IP4kNode, string>("Name", x => x.GetName(), options: new TextColumnOptions<IP4kNode>()
                    {
                        CompareAscending = CompareNodes,
                        CompareDescending = (a, b) => CompareNodes(b, a)
                    }),
                    GetSortedChildren
                ),
                new TextColumn<IP4kNode, string>("Size", x => x.GetSize(), options: new TextColumnOptions<IP4kNode>()
                {
                    CompareAscending = (a, b) => (a?.SizeOrZero() ?? 0).CompareTo(b?.SizeOrZero() ?? 0),
                    CompareDescending = (a, b) => (b?.SizeOrZero() ?? 0).CompareTo(a?.SizeOrZero() ?? 0)
                }),
                new TextColumn<IP4kNode, string>("Date", x => x.GetDate()),
            },
        };

        Source.RowSelection!.SingleSelect = true;
        Source.RowSelection.SelectionChanged += SelectionChanged;
    }
    
    [RelayCommand]
    public void Search()
    {
        ApplySearch();
    }
    
    private void ApplySearch()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            // Reset to show all nodes
            Source.Items = ProcessAndSortNodes(_allRootNodes);
            return;
        }
        
        var searchTerm = SearchText.Trim().ToLowerInvariant();
        
        // Clone the hierarchy but only include matches and their parent folders
        var filteredNodes = CloneAndFilterNodes(_allRootNodes, searchTerm);
        
        Source.Items = GetSortedNodes(filteredNodes);
    }
    
    private List<IP4kNode> CloneAndFilterNodes(ICollection<IP4kNode> nodes, string searchTerm)
    {
        var result = new List<IP4kNode>();
        
        foreach (var node in nodes)
        {
            if (node is P4kFileNode fileNode)
            {
                // For files, check if filename matches
                bool matches = fileNode.ZipEntry.Name.ToLowerInvariant().Contains(searchTerm);
                
                if (matches)
                {
                    result.Add(fileNode);
                }
            }
            else if (node is P4kDirectoryNode dirNode)
            {
                // For folders, check if folder name matches or has matching children
                bool folderMatches = dirNode.Name.ToLowerInvariant().Contains(searchTerm);
                
                var filteredChildren = CloneAndFilterNodes(dirNode.Children.Values, searchTerm);
                
                // Include this folder if it matches or has matching children
                if (folderMatches || filteredChildren.Count > 0)
                {
                    // Create a filtered directory node with only matching children
                    var filteredDirNode = new FilteredP4kDirectoryNode(dirNode.Name, dirNode.Parent, filteredChildren);
                    result.Add(filteredDirNode);
                }
            }
        }
        
        return result;
    }
    
    private async Task ExtractSelectedNode()
    {
        try
        {
            _logger.LogInformation("Extract command executed");
            
            if (Source.RowSelection?.SelectedItem is not IP4kNode selectedNode)
            {
                _logger.LogWarning("No item selected for extraction");
                return;
            }

            // Get the toplevel window for showing the folder picker dialog
            var appLifetime = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
            var topLevel = appLifetime?.MainWindow;
            if (topLevel == null)
            {
                _logger.LogError("Could not get top level window for showing folder picker");
                return;
            }

            // Show folder picker
            var folderPickerOptions = new FolderPickerOpenOptions
            {
                Title = "Select destination folder",
                AllowMultiple = false
            };

            _logger.LogInformation("Opening folder picker dialog");
            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(folderPickerOptions);
            if (folders.Count == 0)
            {
                // User canceled the dialog
                _logger.LogInformation("User canceled folder selection");
                return;
            }

            var destinationFolder = folders[0];
            var destinationPath = destinationFolder.Path.LocalPath;
            _logger.LogInformation("Selected destination: {Path}", destinationPath);

            IsExtracting = true;
            ExtractionProgress = 0;
            
            try
            {
                var progress = new Progress<double>(value => 
                {
                    Dispatcher.UIThread.Post(() => 
                    {
                        ExtractionProgress = value;
                        _logger.LogDebug("Extraction progress: {Progress}%", value * 100);
                    });
                });
                
                var p4kFile = _p4KService.P4KFileSystem.P4kFile as P4kFile;
                if (p4kFile == null) return;
                var extractor = new P4kExtractor(p4kFile);
                
                if (selectedNode is P4kFileNode fileNode)
                {
                    await Task.Run(() =>
                    {
                        // CryXML conversion
                        if (TryConvertCryXml(fileNode, destinationPath))
                        {
                            // done
                        }
                        else if (ConvertDdsToPng && IsDdsFile(fileNode.ZipEntry.Name))
                        {
                            ExtractDdsAsPng(fileNode, destinationPath);
                        }
                        else
                        {
                            extractor.ExtractEntry(destinationPath, fileNode.ZipEntry);
                        }
                        Dispatcher.UIThread.Post(() => ExtractionProgress = 1.0);
                    });
                }
                else if (selectedNode is P4kDirectoryNode dirNode)
                {
                    await Task.Run(() =>
                    {
                        if (ConvertDdsToPng)
                        {
                            ExtractDirectoryWithDdsConversion(dirNode, destinationPath, progress);
                        }
                        else
                        {
                            extractor.ExtractNode(destinationPath, dirNode, progress);
                        }
                    });
                }
                _logger.LogInformation("Extraction completed successfully to {DestinationPath}", destinationPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting to {DestinationPath}", destinationPath);
            }
            finally
            {
                Dispatcher.UIThread.Post(() =>
                {
                    IsExtracting = false;
                    ExtractionProgress = 1.0; // Mark as complete before hiding progress bar
                    _logger.LogInformation("Extraction finally block: IsExtracting set to false.");
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in ExtractSelectedNode");
            Dispatcher.UIThread.Post(() =>
            {
                IsExtracting = false;
                ExtractionProgress = 0.0; // Reset progress on outer error
                _logger.LogInformation("Outer catch block: IsExtracting set to false.");
            });
        }
    }
    
    private bool IsDdsFile(string fileName)
    {
        return fileName.EndsWith(".dds", StringComparison.OrdinalIgnoreCase) || 
               (fileName.Contains(".dds.") && char.IsDigit(fileName[fileName.Length - 1]));
    }
    
    private void ExtractDdsAsPng(P4kFileNode fileNode, string destinationPath)
    {
        try
        {
            _logger.LogInformation("Converting DDS to PNG: {FileName}", fileNode.ZipEntry.Name);
            
            // Preserve the directory structure for the PNG file
            string relativePath = fileNode.ZipEntry.Name;
            string relativeDirectory = Path.GetDirectoryName(relativePath)?.Replace('\\', Path.DirectorySeparatorChar) ?? string.Empty;
            string fileName = Path.GetFileName(relativePath);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            string pngFileName = fileNameWithoutExtension + ".png";
            
            // Create full output path maintaining directory structure
            string outputDirectory = Path.Combine(destinationPath, relativeDirectory);
            string outputPath = Path.Combine(outputDirectory, pngFileName);
            
            // Create directory structure if it doesn't exist
            Directory.CreateDirectory(outputDirectory);
            
            // Process the DDS file
            using var ms = DdsFile.MergeToStream(fileNode.ZipEntry.Name, _p4KService.P4KFileSystem);
            
            // Read the stream into a byte array
            byte[] ddsBytes;
            using (var memoryStream = new MemoryStream())
            {
                ms.CopyTo(memoryStream);
                ddsBytes = memoryStream.ToArray();
            }
            
            // Convert DDS to PNG
            using var pngBytes = DdsFile.ConvertToPng(ddsBytes);
            
            // Save the PNG
            using (var fs = new FileStream(outputPath, FileMode.Create))
            {
                pngBytes.CopyTo(fs);
            }
            
            _logger.LogInformation("Successfully saved PNG: {OutputPath}", outputPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert DDS to PNG: {FileName}", fileNode.ZipEntry.Name);
            
            // Fallback to extracting the original DDS file
            _logger.LogInformation("Falling back to extracting original DDS file");
            var p4kFile = _p4KService.P4KFileSystem.P4kFile as P4kFile;
            if (p4kFile != null)
            {
                var extractor = new P4kExtractor(p4kFile);
                extractor.ExtractEntry(destinationPath, fileNode.ZipEntry);
            }
            else
            {
                _logger.LogError("Failed to get P4kFile instance for fallback extraction");
            }
        }
    }
    
    private void ExtractDirectoryWithDdsConversion(P4kDirectoryNode dirNode, string destinationPath, IProgress<double> progress)
    {
        // Collect all entries to extract
        var entries = dirNode.CollectEntries().ToList();
        var totalEntries = entries.Count;
        var processedEntries = 0;
        
        // Create the directory
        var outputDirPath = Path.Combine(destinationPath, dirNode.Name);
        Directory.CreateDirectory(outputDirPath);
        
        // Get P4kFile instance safely
        var p4kFile = _p4KService.P4KFileSystem.P4kFile as P4kFile;
        if (p4kFile == null)
        {
            _logger.LogError("Failed to get P4kFile instance");
            return;
        }
        
        // Create a single P4kExtractor instance
        var extractor = new P4kExtractor(p4kFile);
        
        // Extract files with DDS conversion
        foreach (var entry in entries)
        {
            try
            {
                var fileName = entry.Name;
                if (IsDdsFile(fileName))
                {
                    var fileNode = FindFileNode(dirNode, fileName);
                    if (fileNode != null) ExtractDdsAsPng(fileNode, outputDirPath);
                }
                else
                {
                    var fileNode = FindFileNode(dirNode, fileName);
                    if (fileNode != null && TryConvertCryXml(fileNode, outputDirPath))
                    {
                        // done
                    }
                    else
                    {
                        extractor.ExtractEntry(outputDirPath, entry);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting file: {FileName}", entry.Name);
            }
            
            // Update progress
            processedEntries++;
            progress.Report(processedEntries / (double)totalEntries);
        }
    }
    
    private P4kFileNode? FindFileNode(P4kDirectoryNode dirNode, string fileName)
    {
        // Find the path parts
        var pathParts = fileName.Split('\\');
        var currentNode = dirNode;
        
        // Navigate the tree
        for (int i = 0; i < pathParts.Length - 1; i++)
        {
            if (currentNode.Children.TryGetValue(pathParts[i], out var childNode) && 
                childNode is P4kDirectoryNode nextDirNode)
            {
                currentNode = nextDirNode;
            }
            else
            {
                return null; // Directory not found
            }
        }
        
        // Get the file node
        if (currentNode.Children.TryGetValue(pathParts[^1], out var fileNode) && 
            fileNode is P4kFileNode p4kFileNode)
        {
            return p4kFileNode;
        }
        
        return null; // File not found
    }
    
    private static void SearchNodes(P4kDirectoryNode currentNode, string searchText, List<IP4kNode> results)
    {
        foreach (var child in currentNode.Children.Values)
        {
            if (child is P4kFileNode fileNode)
            {
                if (fileNode.ZipEntry.Name.ToLowerInvariant().Contains(searchText))
                {
                    results.Add(fileNode);
                }
            }
            else if (child is P4kDirectoryNode dirNode)
            {
                if (dirNode.Name.ToLowerInvariant().Contains(searchText))
                {
                    results.Add(dirNode);
                }
                
                // Continue searching in subdirectories
                SearchNodes(dirNode, searchText, results);
            }
        }
    }
    
    private static int CompareNodes(IP4kNode? a, IP4kNode? b)
    {
        if (a == null && b == null) return 0;
        if (a == null) return -1;
        if (b == null) return 1;

        // Directories come before files
        bool aIsDir = a is P4kDirectoryNode or FilteredP4kDirectoryNode or SocPakDirectoryAdapter;
        bool bIsDir = b is P4kDirectoryNode or FilteredP4kDirectoryNode or SocPakDirectoryAdapter;

        if (aIsDir && !bIsDir) return -1;
        if (!aIsDir && bIsDir) return 1;

        // Both are the same type, sort by name
        return string.Compare(a.GetName(), b.GetName(), StringComparison.OrdinalIgnoreCase);
    }

    private IP4kNode[] GetSortedChildren(IP4kNode node)
    {
        var children = node.GetChildren();
        
        // Process children to replace SOCPAK files with container nodes
        var processedChildren = new List<IP4kNode>();
        
        foreach (var child in children)
        {
            if (child.IsSocPakFile())
            {
                var socPakContainer = new SocPakContainerNode((P4kFileNode)child, GetP4kFileStreamProvider());
                processedChildren.Add(socPakContainer);
            }
            else
            {
                processedChildren.Add(child);
            }
        }
        
        return GetSortedNodes(processedChildren);
    }
    
    private Func<P4kFileNode, Stream> GetP4kFileStreamProvider()
    {
        return (fileNode) => _p4KService.P4KFileSystem.OpenRead(fileNode.ZipEntry.Name);
    }
    
    private IP4kNode[] ProcessAndSortNodes(ICollection<IP4kNode> nodes)
    {
        // Process nodes to replace SOCPAK files with container nodes
        var processedNodes = new List<IP4kNode>();
        
        foreach (var node in nodes)
        {
            if (node.IsSocPakFile())
            {
                var socPakContainer = new SocPakContainerNode((P4kFileNode)node, GetP4kFileStreamProvider());
                processedNodes.Add(socPakContainer);
            }
            else
            {
                processedNodes.Add(node);
            }
        }
        
        return GetSortedNodes(processedNodes);
    }

    private static IP4kNode[] GetSortedNodes(ICollection<IP4kNode> nodes)
    {
        return nodes.OrderBy(n => n is not (P4kDirectoryNode or FilteredP4kDirectoryNode or SocPakDirectoryAdapter)) // Directories first
            .ThenBy(n => n.GetName(), StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private void SelectionChanged(object? sender, TreeSelectionModelSelectionChangedEventArgs<IP4kNode> e)
    {
        if (e.SelectedItems.Count != 1)
            return;

        //TODO: here, set some boolean that locks the entire UI until the preview is loaded.
        //That is needed to prevent race conditions where the user clicks on another file before the preview is loaded.

        //TODO: tabs?

        var selectedEntry = e.SelectedItems[0];
        if (selectedEntry == null)
        {
            Preview = null;
            return;
        }

        // Handle both P4K files and SOCPAK files
        if (selectedEntry is P4kFileNode selectedP4kFile)
        {
        //todo: for a big ass file show a loading screen or something
        Preview = null;
        Task.Run(() =>
        {
            try
            {
                    var p = _previewService.GetPreview(selectedP4kFile);
                Dispatcher.UIThread.Post(() => Preview = p);
            }
            catch (Exception exception)
            {
                    _logger.LogError(exception, "Failed to preview file: {FileName}", selectedP4kFile.ZipEntry.Name);
                Dispatcher.UIThread.Post(() => Preview = new TextPreviewViewModel($"Failed to preview file: {exception.Message}"));
            }
            finally { }
        });
        }
        else if (selectedEntry is SocPakFileAdapter selectedSocPakFile)
        {
            Preview = null;
            Task.Run(() =>
            {
                try
                {
                    var p = GetSocPakFilePreview(selectedSocPakFile);
                    Dispatcher.UIThread.Post(() => Preview = p);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Failed to preview SOCPAK file: {FileName}", selectedSocPakFile.SocPakNode.Name);
                    Dispatcher.UIThread.Post(() => Preview = new TextPreviewViewModel($"Failed to preview SOCPAK file: {exception.Message}"));
                }
                finally { }
            });
        }
        else
        {
            //we clicked on a folder or other non-file node, do nothing to the preview.
            return;
        }
    }

    private FilePreviewViewModel GetSocPakFilePreview(SocPakFileAdapter socPakFileAdapter)
    {
        var socPakFile = socPakFileAdapter.SocPakNode;
        var fileName = socPakFile.Name;
        var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
        
        // Get the SOCPAK container to access the SocPakFile
        var container = FindSocPakContainer(socPakFileAdapter);
        if (container?.SocPakFile == null)
        {
            return new TextPreviewViewModel("SOCPAK file not loaded or not available for preview");
        }
        
        try
        {
            _logger.LogDebug("Attempting to open SOCPAK file: {FilePath}", socPakFile.FullPath);
            
            // Try using the file system approach first
            try
            {
                var socPakFileSystem = new SocPakFileSystem(container.SocPakFile);
                using var entryStream = socPakFileSystem.OpenRead(socPakFile.FullPath);
                return ProcessSocPakFileStream(entryStream, fileName, fileExtension);
            }
            catch (Exception fsEx)
            {
                _logger.LogDebug("FileSystem approach failed: {Error}, trying direct ZipEntry access", fsEx.Message);
                
                // Fallback to direct ZipEntry access
                using var entryStream = container.SocPakFile.OpenStream(socPakFile.ZipEntry);
                return ProcessSocPakFileStream(entryStream, fileName, fileExtension);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open file from SOCPAK: {FileName}", fileName);
            return new TextPreviewViewModel($"Failed to open file from SOCPAK: {ex.Message}");
        }
    }
    
    private FilePreviewViewModel ProcessSocPakFileStream(Stream entryStream, string fileName, string fileExtension)
    {
        try
        {
            // First, read the entire stream into a MemoryStream to avoid issues with compressed ZIP streams
            // that don't support Length/Position operations
            using var memoryStream = new MemoryStream();
            entryStream.CopyTo(memoryStream);
            memoryStream.Position = 0;
            
            // Use similar logic to the existing PreviewService
            var plaintextExtensions = new[] { ".cfg", ".xml", ".txt", ".json", "eco", ".ini" };
            var ddsLodExtensions = new[] { ".dds" };
            var bitmapExtensions = new[] { ".bmp", ".jpg", ".jpeg", ".png" };
            
            // Check CryXML first
            if (StarBreaker.CryXmlB.CryXml.IsCryXmlB(memoryStream))
            {
                memoryStream.Position = 0;
                if (!StarBreaker.CryXmlB.CryXml.TryOpen(memoryStream, out var cryXml))
                {
                    return new TextPreviewViewModel("Failed to open CryXmlB", fileExtension);
                }
                return new TextPreviewViewModel(cryXml.ToString(), ".xml");
            }
            else if (plaintextExtensions.Any(p => fileName.EndsWith(p, StringComparison.InvariantCultureIgnoreCase)))
            {
                memoryStream.Position = 0;
                return new TextPreviewViewModel(memoryStream.ReadString(), fileExtension);
            }
            else if (ddsLodExtensions.Any(p => fileName.EndsWith(p, StringComparison.InvariantCultureIgnoreCase)))
            {
                try
                {
                    // For DDS files in SOCPAK, try to load them directly
                    var ddsBytes = memoryStream.ToArray();
                    using var pngStream = DdsFile.ConvertToPng(ddsBytes);
                    return new DdsPreviewViewModel(new Bitmap(pngStream));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to convert DDS file from SOCPAK: {FileName}", fileName);
                    return new TextPreviewViewModel($"Failed to preview DDS file: {ex.Message}", fileExtension);
                }
            }
            else if (bitmapExtensions.Any(p => fileName.EndsWith(p, StringComparison.InvariantCultureIgnoreCase)))
            {
                try
                {
                    memoryStream.Position = 0;
                    return new DdsPreviewViewModel(new Bitmap(memoryStream));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load bitmap from SOCPAK: {FileName}", fileName);
                    return new TextPreviewViewModel($"Failed to preview bitmap: {ex.Message}", fileExtension);
                }
            }
            else
            {
                try
                {
                    return new HexPreviewViewModel(memoryStream.ToArray());
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create hex preview from SOCPAK: {FileName}", fileName);
                    return new TextPreviewViewModel($"Failed to create hex preview: {ex.Message}", fileExtension);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process SOCPAK file stream: {FileName}", fileName);
            return new TextPreviewViewModel($"Failed to process SOCPAK file stream: {ex.Message}");
        }
    }
    
    private SocPakContainerNode? FindSocPakContainer(SocPakFileAdapter socPakFileAdapter)
    {
        // Walk up the parent hierarchy to find the SOCPAK container
        var current = socPakFileAdapter.Parent;
        while (current != null)
        {
            if (current is SocPakContainerNode container)
                return container;
            if (current is SocPakDirectoryAdapter adapter)
                current = adapter.Parent;
            else
                break;
        }
        return null;
    }

    private bool TryConvertCryXml(P4kFileNode fileNode, string destinationPath)
    {
        try
        {
            using var entryStream = _p4KService.P4KFileSystem.OpenRead(fileNode.ZipEntry.Name);
            var ms = new MemoryStream();
            entryStream.CopyTo(ms);
            ms.Position = 0;
            if (!CryXml.IsCryXmlB(ms)) return false;
            ms.Position = 0;
            if (!CryXml.TryOpen(ms, out var cryXml)) return false;
            _logger.LogInformation("Converting CryXML to text: {FileName}", fileNode.ZipEntry.Name);
            var relativePath = fileNode.ZipEntry.Name;
            var relativeDirectory = Path.GetDirectoryName(relativePath)?.Replace('\\', Path.DirectorySeparatorChar) ?? string.Empty;
            var outputDirectory = Path.Combine(destinationPath, relativeDirectory);
            Directory.CreateDirectory(outputDirectory);
            var outName = Path.GetFileName(relativePath);
            var outPath = Path.Combine(outputDirectory, outName);
            File.WriteAllText(outPath, cryXml.ToString());
            _logger.LogInformation("Successfully converted CryXML to: {OutPath}", outPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert CryXML for {FileName}", fileNode.ZipEntry.Name);
            return false;
        }
    }
}