using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Selection;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using StarBreaker.Extensions;
using StarBreaker.P4k;
using StarBreaker.Services;

namespace StarBreaker.Screens;

public sealed partial class P4kTabViewModel : PageViewModelBase
{
    public override string Name => "P4k";
    public override string Icon => "ZipFolder";

    private readonly ILogger<P4kTabViewModel> _logger;
    private readonly IP4kService _p4KService;
    private readonly IPreviewService _previewService;

    [ObservableProperty] private HierarchicalTreeDataGridSource<IP4kNode> _source;
    [ObservableProperty] private FilePreviewViewModel? _preview;
    [ObservableProperty] private string _searchText = string.Empty;
    
    private ICollection<IP4kNode> _allRootNodes;

    public P4kTabViewModel(IP4kService p4kService, ILogger<P4kTabViewModel> logger, IPreviewService previewService)
    {
        _p4KService = p4kService;
        _logger = logger;
        _previewService = previewService;
        
        InitializeTreeDataGrid();
        
        _allRootNodes = _p4KService.P4KFileSystem.Root.Children.Values;
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
    }
    
    [RelayCommand]
    public void Search()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            // Reset to show all nodes
            Source.Items = GetSortedNodes(_allRootNodes);
            return;
        }
        
        var searchResults = new List<IP4kNode>();
        SearchNodes(_p4KService.P4KFileSystem.Root, SearchText.ToLowerInvariant(), searchResults);
        Source.Items = GetSortedNodes(searchResults);
    }
    
    public async Task ExtractSelectedNode()
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

            try
            {
                if (selectedNode is P4kFileNode fileNode)
                {
                    // Extract a single file
                    _logger.LogInformation("Extracting file: {FileName}", fileNode.ZipEntry.Name);
                    ExtractFile(fileNode, destinationPath);
                }
                else if (selectedNode is P4kDirectoryNode dirNode)
                {
                    // Extract a directory
                    _logger.LogInformation("Extracting directory: {DirectoryName}", dirNode.Name);
                    ExtractDirectory(dirNode, destinationPath);
                }
                
                _logger.LogInformation("Extraction completed successfully to {DestinationPath}", destinationPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting to {DestinationPath}", destinationPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in ExtractSelectedNode");
        }
    }
    
    private void ExtractFile(P4kFileNode fileNode, string destinationPath)
    {
        var fileName = Path.GetFileName(fileNode.ZipEntry.Name);
        var outputPath = Path.Combine(destinationPath, fileName);
        
        // Create directory if it doesn't exist
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        
        // Extract file content
        var fileContent = _p4KService.P4KFileSystem.ReadAllBytes(fileNode.ZipEntry.Name);
        File.WriteAllBytes(outputPath, fileContent);
    }
    
    private void ExtractDirectory(P4kDirectoryNode dirNode, string destinationPath)
    {
        // Create base directory for extraction
        var directoryPath = Path.Combine(destinationPath, dirNode.Name);
        Directory.CreateDirectory(directoryPath);
        
        // Extract all files in this directory
        ExtractDirectoryContents(dirNode, directoryPath);
    }
    
    private void ExtractDirectoryContents(P4kDirectoryNode dirNode, string destinationPath)
    {
        foreach (var child in dirNode.Children.Values)
        {
            if (child is P4kFileNode fileNode)
            {
                var fileName = Path.GetFileName(fileNode.ZipEntry.Name);
                var outputPath = Path.Combine(destinationPath, fileName);
                
                // Extract file content
                var fileContent = _p4KService.P4KFileSystem.ReadAllBytes(fileNode.ZipEntry.Name);
                File.WriteAllBytes(outputPath, fileContent);
            }
            else if (child is P4kDirectoryNode subDirNode)
            {
                // Create subdirectory
                var subDirPath = Path.Combine(destinationPath, subDirNode.Name);
                Directory.CreateDirectory(subDirPath);
                
                // Recursively extract subdirectory contents
                ExtractDirectoryContents(subDirNode, subDirPath);
            }
        }
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
        bool aIsDir = a is P4kDirectoryNode;
        bool bIsDir = b is P4kDirectoryNode;

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
        return nodes.OrderBy(n => n is not P4kDirectoryNode) // Directories first
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

        if (selectedEntry is not P4kFileNode selectedFile)
        {
            //we clicked on a folder, do nothing to the preview.
            return;
        }

        if (selectedFile.ZipEntry.UncompressedSize > int.MaxValue)
        {
            _logger.LogWarning("File too big to preview");
            return;
        }

        //todo: for a big ass file show a loading screen or something
        Preview = null;
        Task.Run(() =>
        {
            try
            {
                var p = _previewService.GetPreview(selectedFile);
                Dispatcher.UIThread.Post(() => Preview = p);
            }
            // catch (Exception exception)
            // {
            //     _logger.LogError(exception, "Failed to preview file");
            // }
            finally { }
        });
    }
}