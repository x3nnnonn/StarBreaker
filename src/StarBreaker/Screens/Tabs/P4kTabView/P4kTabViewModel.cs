using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Selection;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using StarBreaker.CryXmlB;
using StarBreaker.Dds;
using StarBreaker.Extensions;
using StarBreaker.P4k;
using StarBreaker.P4k.Extraction;
using StarBreaker.Services;
using System.IO;

namespace StarBreaker.Screens;

// Helper class for filtered directory display during search
public class FilteredP4kDirectoryNode : IP4kNode
{
    public IP4kNode Parent { get; }
    public string Name { get; }
    public ICollection<IP4kNode> FilteredChildren { get; }

    // IP4kNode implementation
    public IP4kFile P4k => Parent.P4k;
    public P4kRoot Root => Parent.Root;
    public ulong Size
    {
        get
        {
            ulong total = 0;
            foreach (var child in FilteredChildren)
                total += child.Size;
            return total;
        }
    }

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
        
        _allRootNodes = _p4KService.P4KFileSystem.Children.Values;
        Source.Items = GetSortedNodes(_allRootNodes);
        
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
        Source.Items = GetSortedNodes(_p4KService.P4KFileSystem.Children.Values);
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
            Source.Items = GetSortedNodes(_allRootNodes);
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
                bool matches = fileNode.P4KEntry.Name.ToLowerInvariant().Contains(searchTerm);
                
                if (matches)
                {
                    result.Add(fileNode);
                }
            }
            else if (node is P4kSocPakFileNode socPakNode)
            {
                // For SOCPAK files, only check if SOCPAK name matches (don't search children to avoid blocking I/O)
                bool socPakMatches = socPakNode.Name.ToLowerInvariant().Contains(searchTerm) ||
                                   socPakNode.P4KEntry.Name.ToLowerInvariant().Contains(searchTerm);
                
                // Include this SOCPAK if it matches
                if (socPakMatches)
                {
                    result.Add(socPakNode);
                }
            }
            else if (node is P4kSocPakDirectoryNode socPakDirNode)
            {
                // For SOCPAK directories, check if directory name matches or has matching children
                bool dirMatches = socPakDirNode.Name.ToLowerInvariant().Contains(searchTerm);
                
                var filteredChildren = CloneAndFilterNodes(socPakDirNode.Children.Values, searchTerm);
                
                // Include this directory if it matches or has matching children
                if (dirMatches || filteredChildren.Count > 0)
                {
                    result.Add(socPakDirNode);
                }
            }
            else if (node is P4kSocPakChildFileNode socPakChildNode)
            {
                // For files inside SOCPAK, check if filename matches
                bool matches = socPakChildNode.P4KEntry.Name.ToLowerInvariant().Contains(searchTerm);
                
                if (matches)
                {
                    result.Add(socPakChildNode);
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
                    var filteredDirNode = new FilteredP4kDirectoryNode(dirNode.Name, dirNode, filteredChildren);
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
                
                var p4kFile = _p4KService.P4KFileSystem.P4k as P4kFile;
                if (p4kFile == null) return;
                var extractor = new StarBreaker.P4k.Extraction.P4kExtractor(p4kFile);
                
                if (selectedNode is P4kFileNode fileNode)
                {
                    await Task.Run(() =>
                    {
                        // CryXML conversion
                        if (TryConvertCryXml(fileNode, destinationPath))
                        {
                            // done
                        }
                        else if (ConvertDdsToPng && IsDdsFile(fileNode.P4KEntry.Name))
                        {
                            ExtractDdsAsJpeg(fileNode, destinationPath);
                        }
                        else
                        {
                            extractor.ExtractEntry(destinationPath, fileNode.P4KEntry);
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
    
    private void ExtractDdsAsJpeg(P4kFileNode fileNode, string destinationPath)
    {
        try
        {
            _logger.LogInformation("Converting DDS to PNG: {FileName}", fileNode.P4KEntry.Name);
            
            // Preserve the directory structure for the PNG file
            string relativePath = fileNode.P4KEntry.Name;
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
            using var ms = DdsFile.MergeToStream(fileNode.P4KEntry.Name, _p4KService.P4KFileSystem);
            
            // Read the stream into a byte array
            byte[] ddsBytes;
            using (var memoryStream = new MemoryStream())
            {
                ms.CopyTo(memoryStream);
                ddsBytes = memoryStream.ToArray();
            }
            
            // Convert DDS to PNG (remove alpha, apply sRGB correction for mobile)
            using var pngBytes = DdsFile.ConvertToPng(ddsBytes, true, true);
            
            // Save the PNG
            using (var fs = new FileStream(outputPath, FileMode.Create))
            {
                pngBytes.CopyTo(fs);
            }
            
            _logger.LogInformation("Successfully saved PNG: {OutputPath}", outputPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert DDS to PNG: {FileName}", fileNode.P4KEntry.Name);
            
            // Fallback to extracting the original DDS file
            _logger.LogInformation("Falling back to extracting original DDS file");
            var p4kFile = _p4KService.P4KFileSystem.P4k as P4kFile;
            if (p4kFile != null)
            {
                var extractor = new StarBreaker.P4k.Extraction.P4kExtractor(p4kFile);
                extractor.ExtractEntry(destinationPath, fileNode.P4KEntry);
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
        var p4kFile = _p4KService.P4KFileSystem.P4k as P4kFile;
        if (p4kFile == null)
        {
            _logger.LogError("Failed to get P4kFile instance");
            return;
        }
        
        // Create a single P4kExtractor instance
        var extractor = new StarBreaker.P4k.Extraction.P4kExtractor(p4kFile);
        
        // Extract files with DDS conversion
        foreach (var entry in entries)
        {
            try
            {
                var fileName = entry.Name;
                if (IsDdsFile(fileName))
                {
                    var fileNode = FindFileNode(dirNode, fileName);
                    if (fileNode != null) ExtractDdsAsJpeg(fileNode, outputDirPath);
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
                if (fileNode.P4KEntry.Name.ToLowerInvariant().Contains(searchText))
                {
                    results.Add(fileNode);
                }
            }
            else if (child is P4kSocPakFileNode socPakNode)
            {
                // Check if SOCPAK itself matches (don't search children to avoid blocking I/O)
                if (socPakNode.Name.ToLowerInvariant().Contains(searchText) ||
                    socPakNode.P4KEntry.Name.ToLowerInvariant().Contains(searchText))
                {
                    results.Add(socPakNode);
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

        // Directories and SOCPAK files (which act as expandable containers) come before regular files
        bool aIsDir = a is P4kDirectoryNode or FilteredP4kDirectoryNode or P4kSocPakFileNode or P4kSocPakDirectoryNode;
        bool bIsDir = b is P4kDirectoryNode or FilteredP4kDirectoryNode or P4kSocPakFileNode or P4kSocPakDirectoryNode;

        if (aIsDir && !bIsDir) return -1;
        if (!aIsDir && bIsDir) return 1;

        // Both are the same type, sort by name
        return string.Compare(a.GetName(), b.GetName(), StringComparison.OrdinalIgnoreCase);
    }

    private static IP4kNode[] GetSortedChildren(IP4kNode node)
    {
        var children = node.GetChildren();
        return GetSortedNodes(children);
    }

    private static IP4kNode[] GetSortedNodes(ICollection<IP4kNode> nodes)
    {
        return nodes.OrderBy(n => n is not (P4kDirectoryNode or FilteredP4kDirectoryNode or P4kSocPakFileNode or P4kSocPakDirectoryNode)) // Directories and SOCPAK containers first
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

        // Handle different node types for preview
        if (selectedEntry is P4kFileNode selectedFile)
        {
        //todo: for a big ass file show a loading screen or something
        Preview = null;
        Task.Run(() =>
        {
            try
            {
                var p = _previewService.GetPreview(selectedFile);
                Dispatcher.UIThread.Post(() => Preview = p);
            }
            catch (Exception exception)
            {
                    _logger.LogError(exception, "Failed to preview file: {FileName}", selectedFile.P4KEntry.Name);
                Dispatcher.UIThread.Post(() => Preview = new TextPreviewViewModel($"Failed to preview file: {exception.Message}"));
            }
            finally { }
        });
        }
        else if (selectedEntry is P4kSocPakChildFileNode socPakChildFile)
        {
            //todo: for a big ass file show a loading screen or something
            Preview = null;
            Task.Run(() =>
            {
                try
                {
                    var p = _previewService.GetSocPakFilePreview(socPakChildFile);
                    Dispatcher.UIThread.Post(() => Preview = p);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Failed to preview SOCPAK file: {FileName}", socPakChildFile.P4KEntry.Name);
                    Dispatcher.UIThread.Post(() => Preview = new TextPreviewViewModel($"Failed to preview SOCPAK file: {exception.Message}"));
                }
                finally { }
            });
        }
        else
        {
            //we clicked on a folder or SOCPAK container, do nothing to the preview.
            return;
        }
    }

    private bool TryConvertCryXml(P4kFileNode fileNode, string destinationPath)
    {
        try
        {
            using var entryStream = _p4KService.P4KFileSystem.OpenRead(fileNode.P4KEntry.Name);
            var ms = new MemoryStream();
            entryStream.CopyTo(ms);
            ms.Position = 0;
            if (!CryXml.IsCryXmlB(ms)) return false;
            ms.Position = 0;
            if (!CryXml.TryOpen(ms, out var cryXml)) return false;
            _logger.LogInformation("Converting CryXML to text: {FileName}", fileNode.P4KEntry.Name);
            var relativePath = fileNode.P4KEntry.Name;
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
            _logger.LogError(ex, "Failed to convert CryXML for {FileName}", fileNode.P4KEntry.Name);
            return false;
        }
    }
}