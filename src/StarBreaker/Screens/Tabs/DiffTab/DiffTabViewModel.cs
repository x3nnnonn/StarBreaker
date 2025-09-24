using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using StarBreaker.DataCore;
using StarBreaker.P4k;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using ZstdSharp;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Selection;
using StarBreaker.Extensions;
using StarBreaker.Services;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using StarBreaker.Common;
using StarBreaker.Dds;
using Avalonia.Media.Imaging;
using StarBreaker.Protobuf;

namespace StarBreaker.Screens;

public sealed partial class DiffTabViewModel : PageViewModelBase
{
    public override string Name => "Diff";
    public override string Icon => "Compare";

    private readonly ILogger<DiffTabViewModel> _logger;

    [ObservableProperty] private string _gameFolder = string.Empty;
    [ObservableProperty] private string _outputDirectory = string.Empty;
    [ObservableProperty] private bool _keepOldFiles;
    [ObservableProperty] private string _textFormat = "xml";
    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private double _progress;
    [ObservableProperty] private string _currentStatus = "Ready";
    [ObservableProperty] private ObservableCollection<string> _logMessages = new();
    [ObservableProperty] private string _selectedChannel = "LIVE";
    [ObservableProperty] private bool _isLiveSelected = true;
    [ObservableProperty] private bool _isPtuSelected = false;
    [ObservableProperty] private bool _isEptuSelected = false;
    [ObservableProperty] private bool _isTechPreviewSelected = false;
    
    // Comparison mode properties
    [ObservableProperty] private bool _isP4kComparisonMode = false;
    [ObservableProperty] private bool _isDataCoreComparisonMode = false;
    [ObservableProperty] private string _leftP4kPath = string.Empty;
    [ObservableProperty] private string _rightP4kPath = string.Empty;
    [ObservableProperty] private string _leftDataCoreP4kPath = string.Empty;
    [ObservableProperty] private string _rightDataCoreP4kPath = string.Empty;
    [ObservableProperty] private string _p4kOutputDirectory = string.Empty;
    [ObservableProperty] private HierarchicalTreeDataGridSource<IP4kComparisonNode>? _comparisonSource;
    [ObservableProperty] private HierarchicalTreeDataGridSource<IDataCoreComparisonNode>? _dataCoreComparisonSource;
    [ObservableProperty] private bool _isComparing = false;
    [ObservableProperty] private string _comparisonStatus = "Ready";
    [ObservableProperty] private FilePreviewViewModel? _preview;
    [ObservableProperty] private bool _showNoSelectionMessage = true;
    [ObservableProperty] private bool _showOnlyChangedFiles = true;
    
    // P4K files for preview
    private P4kFile? _leftP4kFile;
    private P4kFile? _rightP4kFile;
    private P4kComparisonDirectoryNode? _comparisonRoot;
    
    // DataCore databases for preview
    private DataCoreDatabase? _leftDataCoreDatabase;
    private DataCoreDatabase? _rightDataCoreDatabase;
    private DataCoreComparisonDirectoryNode? _dataCoreComparisonRoot;
    private Dictionary<string, string>? _currentLocalizationData;

    // Selected items tracking
    [ObservableProperty] private IList<IP4kComparisonNode> _selectedP4kFiles = new List<IP4kComparisonNode>();
    [ObservableProperty] private IList<IDataCoreComparisonNode> _selectedDataCoreFiles = new List<IDataCoreComparisonNode>();

    public DiffTabViewModel(ILogger<DiffTabViewModel> logger)
    {
        _logger = logger;
        LoadSettings();
        InitializeComparisonTreeDataGrid();
        InitializeDataCoreComparisonTreeDataGrid();
    }

    private void InitializeComparisonTreeDataGrid()
    {
        ComparisonSource = new HierarchicalTreeDataGridSource<IP4kComparisonNode>(Array.Empty<IP4kComparisonNode>())
        {
            Columns =
            {
                new HierarchicalExpanderColumn<IP4kComparisonNode>(
                    new TemplateColumn<IP4kComparisonNode>("Name", "NameCellTemplate", null, new GridLength(1, GridUnitType.Star)),
                    node => GetComparisonChildren(node)
                ),
                new TemplateColumn<IP4kComparisonNode>("Status", "StatusCellTemplate", null, new GridLength(100)),
                new TemplateColumn<IP4kComparisonNode>("Size", "SizeCellTemplate", null, new GridLength(150)),
                new TemplateColumn<IP4kComparisonNode>("Date", "DateCellTemplate", null, new GridLength(150)),
            },
        };

        ComparisonSource.RowSelection!.SingleSelect = false; // Enable multi-selection
        ComparisonSource.RowSelection!.SelectionChanged += OnComparisonSelectionChanged;
    }
    
    private void InitializeDataCoreComparisonTreeDataGrid()
    {
        DataCoreComparisonSource = new HierarchicalTreeDataGridSource<IDataCoreComparisonNode>(Array.Empty<IDataCoreComparisonNode>())
        {
            Columns =
            {
                new HierarchicalExpanderColumn<IDataCoreComparisonNode>(
                    new TemplateColumn<IDataCoreComparisonNode>("Name", "DataCoreNameCellTemplate", null, new GridLength(1, GridUnitType.Star)),
                    node => GetDataCoreComparisonChildren(node)
                ),
                new TemplateColumn<IDataCoreComparisonNode>("Status", "DataCoreStatusCellTemplate", null, new GridLength(100)),
                new TemplateColumn<IDataCoreComparisonNode>("Size", "DataCoreSizeCellTemplate", null, new GridLength(150)),
                new TemplateColumn<IDataCoreComparisonNode>("Type", "DataCoreTypeCellTemplate", null, new GridLength(150)),
            },
        };

        DataCoreComparisonSource.RowSelection!.SingleSelect = false; // Enable multi-selection
        DataCoreComparisonSource.RowSelection!.SelectionChanged += OnDataCoreComparisonSelectionChanged;
    }
    
    private IP4kComparisonNode[] GetComparisonChildren(IP4kComparisonNode node)
    {
        var children = node.GetFilteredComparisonChildren(ShowOnlyChangedFiles);
        return children.OrderBy(n => n is not P4kComparisonDirectoryNode) // Directories first
            .ThenBy(n => n.GetComparisonName(), StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
    
    private IDataCoreComparisonNode[] GetDataCoreComparisonChildren(IDataCoreComparisonNode node)
    {
        var children = node.GetFilteredComparisonChildren(ShowOnlyChangedFiles);
        return children.OrderBy(n => n is not DataCoreComparisonDirectoryNode) // Directories first
            .ThenBy(n => n.GetComparisonName(), StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private void OnComparisonSelectionChanged(object? sender, TreeSelectionModelSelectionChangedEventArgs<IP4kComparisonNode> e)
    {
        // Update selected files list
        SelectedP4kFiles = e.SelectedItems.Where(item => item != null).ToList()!;
        OnPropertyChanged(nameof(CanExtractSelectedP4kFiles));

        // For preview, only show preview for single selection
        if (e.SelectedItems.Count != 1)
        {
            Preview = null;
            ShowNoSelectionMessage = true;
            return;
        }

        var selectedNode = e.SelectedItems[0];
        if (selectedNode is not P4kComparisonFileNode fileNode)
        {
            // Directory selected, clear preview
            Preview = null;
            ShowNoSelectionMessage = true;
            return;
        }

        // Load preview for the selected file
        ShowNoSelectionMessage = false;
        Preview = null; // Show loading indicator
        
        Task.Run(() =>
        {
            try
            {
                var preview = GetFilePreview(fileNode);
                Dispatcher.UIThread.Post(() => Preview = preview);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to preview file: {FileName}", fileNode.Name);
                Dispatcher.UIThread.Post(() => Preview = new TextPreviewViewModel($"Failed to preview file: {ex.Message}"));
            }
        });
    }
    
    private void OnDataCoreComparisonSelectionChanged(object? sender, TreeSelectionModelSelectionChangedEventArgs<IDataCoreComparisonNode> e)
    {
        // Update selected files list
        SelectedDataCoreFiles = e.SelectedItems.Where(item => item != null).ToList()!;
        OnPropertyChanged(nameof(CanExtractSelectedDataCoreFiles));

        // For preview, only show preview for single selection
        if (e.SelectedItems.Count != 1)
        {
            Preview = null;
            ShowNoSelectionMessage = true;
            return;
        }

        var selectedNode = e.SelectedItems[0];
        if (selectedNode is not DataCoreComparisonFileNode fileNode)
        {
            // Directory selected, clear preview
            Preview = null;
            ShowNoSelectionMessage = true;
            return;
        }

        // Load preview for the selected DataCore file
        ShowNoSelectionMessage = false;
        Preview = null; // Show loading indicator
        
        Task.Run(() =>
        {
            try
            {
                var preview = GetDataCoreFilePreview(fileNode);
                Dispatcher.UIThread.Post(() => Preview = preview);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to preview DataCore file: {FileName}", fileNode.Name);
                Dispatcher.UIThread.Post(() => Preview = new TextPreviewViewModel($"Failed to preview DataCore file: {ex.Message}"));
            }
        });
    }

    private FilePreviewViewModel GetFilePreview(P4kComparisonFileNode fileNode)
    {
        var fileName = fileNode.Name;
        var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
        
        // Use similar logic to the existing PreviewService
        var plaintextExtensions = new[] { ".cfg", ".xml", ".txt", ".json", "eco", ".ini" };
        var ddsLodExtensions = new[] { ".dds" };
        var bitmapExtensions = new[] { ".bmp", ".jpg", ".jpeg", ".png" };

        // For modified text files, show a diff view
        if (fileNode.Status == P4kComparisonStatus.Modified && 
            (plaintextExtensions.Any(p => fileName.EndsWith(p, StringComparison.InvariantCultureIgnoreCase)) ||
             IsCryXmlFile(fileNode)))
        {
            return CreateDiffPreview(fileNode, fileExtension);
        }

        // For non-modified files or non-text files, use the standard preview logic
        // Determine which P4K to read from based on the file status
        P4kFile? sourceP4k = fileNode.Status switch
        {
            P4kComparisonStatus.Added => _rightP4kFile,     // File only exists in right P4K
            P4kComparisonStatus.Removed => _leftP4kFile,   // File only exists in left P4K
            P4kComparisonStatus.Modified => _rightP4kFile, // Show the newer version for non-text files
            P4kComparisonStatus.Unchanged => _leftP4kFile, // Either P4K is fine
            _ => _leftP4kFile
        };

        if (sourceP4k == null)
        {
            return new TextPreviewViewModel("P4K file not available for preview");
        }

        var zipEntry = fileNode.Status switch
        {
            P4kComparisonStatus.Added => fileNode.RightEntry,
            P4kComparisonStatus.Removed => fileNode.LeftEntry,
            P4kComparisonStatus.Modified => fileNode.RightEntry,
            P4kComparisonStatus.Unchanged => fileNode.LeftEntry ?? fileNode.RightEntry,
            _ => fileNode.LeftEntry ?? fileNode.RightEntry
        };

        if (zipEntry == null)
        {
            return new TextPreviewViewModel("File entry not available for preview");
        }

        // Resolve nested archive contexts (socpak/pak) to find correct P4kFile and entry
        var contextP4k = sourceP4k!;
        var entryToOpen = zipEntry!;
        var fullPathParts = fileNode.FullPath.Split('\\');
        // Look for the first archive segment in the full path
        for (int i = 0; i < fullPathParts.Length - 1; i++)
        {
            var part = fullPathParts[i];
            if ((part.EndsWith(".socpak", StringComparison.OrdinalIgnoreCase) || part.EndsWith(".pak", StringComparison.OrdinalIgnoreCase))
                && !part.Contains("shadercache_", StringComparison.OrdinalIgnoreCase))
            {
                // Find the archive entry in the current context
                var archiveEntry = contextP4k.Entries.FirstOrDefault(e => Path.GetFileName(e.Name).Equals(part, StringComparison.OrdinalIgnoreCase));
                if (archiveEntry != null)
                {
                    contextP4k = P4kFile.FromP4kEntry(contextP4k, archiveEntry);
                    // The remaining path inside this archive
                    var nestedPath = string.Join("\\", fullPathParts.Skip(i + 1));
                    // Locate the inner entry by matching the file name directly, ignoring path separators
                    var nestedEntry = contextP4k.Entries.FirstOrDefault(e => Path.GetFileName(e.Name)
                        .Equals(fileNode.Name, StringComparison.OrdinalIgnoreCase));
                    if (nestedEntry != null)
                        entryToOpen = nestedEntry;
                }
                break;
            }
        }

        // Open the resolved entry stream
        Stream entryStream;
        try
        {
            entryStream = contextP4k.OpenStream(entryToOpen);
        }
        catch (Exception ex) when (ex.Message.Contains("Invalid local file header"))
        {
            _logger.LogWarning(ex, "Nested archive read failed, falling back to direct stream for {FullPath}", fileNode.FullPath);
            entryStream = sourceP4k.OpenStream(zipEntry!);
            _logger.LogWarning(ex, "GetFilePreview: OpenStream failed: {Message}, falling back to source.OpenStream", ex.Message);
        }
        _logger.LogInformation("GetFilePreview: stream opened, checking CryXml for '{FileName}'", fileName);
        using (entryStream)

        // Check CryXML before extension since ".xml" sometimes is cxml sometimes plaintext
        if (StarBreaker.CryXmlB.CryXml.IsCryXmlB(entryStream))
        {
            if (!StarBreaker.CryXmlB.CryXml.TryOpen(entryStream, out var c))
            {
                return new TextPreviewViewModel("Failed to open CryXmlB", fileExtension);
            }
            return new TextPreviewViewModel(c.ToString(), ".xml"); // CryXML converts to XML
        }
        else if (plaintextExtensions.Any(p => fileName.EndsWith(p, StringComparison.InvariantCultureIgnoreCase)))
        {
            return new TextPreviewViewModel(entryStream.ReadString(), fileExtension);
        }
        else if (ddsLodExtensions.Any(p => fileName.EndsWith(p, StringComparison.InvariantCultureIgnoreCase)))
        {
            try
            {
                var p4kFileSystem = new P4kFileSystem(sourceP4k);
                var ms = DdsFile.MergeToStream(zipEntry.Name, p4kFileSystem);
                var jpegBytes = DdsFile.ConvertToJpeg(ms.ToArray(), false);
                return new DdsPreviewViewModel(new Bitmap(jpegBytes));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to convert DDS file: {FileName}", zipEntry.Name);
                return new TextPreviewViewModel($"Failed to preview DDS file: {ex.Message}", fileExtension);
            }
        }
        else if (bitmapExtensions.Any(p => fileName.EndsWith(p, StringComparison.InvariantCultureIgnoreCase)))
        {
            try
            {
                return new DdsPreviewViewModel(new Bitmap(entryStream));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load bitmap: {FileName}", zipEntry.Name);
                return new TextPreviewViewModel($"Failed to preview bitmap: {ex.Message}", fileExtension);
            }
        }
        else
        {
            try
            {
                return new HexPreviewViewModel(entryStream.ToArray());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create hex preview: {FileName}", zipEntry.Name);
                return new TextPreviewViewModel($"Failed to create hex preview: {ex.Message}", fileExtension);
            }
        }
    }

    private bool IsCryXmlFile(P4kComparisonFileNode fileNode)
    {
        // Detect CryXmlB by opening with nested-resolution
        if (fileNode.LeftEntry != null && _leftP4kFile != null)
        {
            using var leftStream = OpenEntryStream(fileNode, true);
            if (StarBreaker.CryXmlB.CryXml.IsCryXmlB(leftStream))
                return true;
        }

        if (fileNode.RightEntry != null && _rightP4kFile != null)
        {
            using var rightStream = OpenEntryStream(fileNode, false);
            if (StarBreaker.CryXmlB.CryXml.IsCryXmlB(rightStream))
                return true;
        }

        return false;
    }

    private FilePreviewViewModel CreateDiffPreview(P4kComparisonFileNode fileNode, string fileExtension)
    {
        _logger.LogInformation("CreateDiffPreview start: {FullPath}", fileNode.FullPath);
        if (_leftP4kFile == null || _rightP4kFile == null || 
            fileNode.LeftEntry == null || fileNode.RightEntry == null)
        {
            return new TextPreviewViewModel("Unable to create diff - missing file data");
        }

        try
        {
            // Get old content (left P4K)
            string oldContent;
            // Resolve nested context for left entry
            var leftContext = _leftP4kFile!;
            var leftEntryName = fileNode.FullPath;
            var leftEntryToOpen = fileNode.LeftEntry!;
            var leftParts = leftEntryName.Split('\\');
            for (int i = 0; i < leftParts.Length - 1; i++)
            {
                var part = leftParts[i];
                _logger.LogDebug("CreateDiffPreview [Left]: inspecting part '{Part}'", part);
                if ((part.EndsWith(".socpak", StringComparison.OrdinalIgnoreCase) || part.EndsWith(".pak", StringComparison.OrdinalIgnoreCase))
                    && !part.Contains("shadercache_", StringComparison.OrdinalIgnoreCase))
                {
                    var archiveEntry = leftContext.Entries.FirstOrDefault(e => Path.GetFileName(e.Name).Equals(part, StringComparison.OrdinalIgnoreCase));
                    // Log the archive entry name or 'null' if not found
                    _logger.LogDebug("CreateDiffPreview [Left]: found archiveEntry = '{ArchiveEntry}'", archiveEntry?.Name ?? "null");
                    if (archiveEntry != null)
                    {
                        leftContext = P4kFile.FromP4kEntry(leftContext, archiveEntry);
                        // Locate the nested entry by matching the file name directly, ignoring any path prefixes
                        var nestedEntry = leftContext.Entries.FirstOrDefault(e => Path.GetFileName(e.Name)
                            .Equals(fileNode.Name, StringComparison.OrdinalIgnoreCase));
                        // Log the name or 'null' if not found
                        _logger.LogDebug("CreateDiffPreview [Left]: nestedEntry = '{NestedEntry}'", nestedEntry?.Name ?? "null");
                        if (nestedEntry != null)
                            leftEntryToOpen = nestedEntry;
                    }
                    break;
                }
            }
            Stream leftStream;
            try
            {
                _logger.LogDebug("CreateDiffPreview [Left]: opening stream for '{EntryName}'", leftEntryToOpen.Name);
                leftStream = leftContext.OpenStream(leftEntryToOpen);
            }
            catch (Exception ex) when (ex.Message.Contains("Invalid local file header"))
            {
                _logger.LogWarning(ex, "Nested archive read failed, falling back to direct left stream for {FileName}", fileNode.Name);
                leftStream = _leftP4kFile!.OpenStream(fileNode.LeftEntry!);
                _logger.LogWarning(ex, "CreateDiffPreview [Left]: OpenStream failed: {Message}, falling back", ex.Message);
            }
            using (leftStream)
            {
                if (StarBreaker.CryXmlB.CryXml.IsCryXmlB(leftStream))
                {
                    if (StarBreaker.CryXmlB.CryXml.TryOpen(leftStream, out var cryXml))
                    {
                        oldContent = cryXml.ToString();
                    }
                    else
                    {
                        oldContent = "Failed to parse CryXML";
                    }
                }
                else
                {
                    oldContent = leftStream.ReadString();
                }
            }
            _logger.LogDebug("CreateDiffPreview [Left]: oldContent length {OldContent.Length}", oldContent.Length);

            // Get new content (right P4K)
            string newContent;
            // Resolve nested context for right entry
            var rightContext = _rightP4kFile!;
            var rightEntryName = fileNode.FullPath;
            var rightEntryToOpen = fileNode.RightEntry!;
            var rightParts = rightEntryName.Split('\\');
            for (int i = 0; i < rightParts.Length - 1; i++)
            {
                var part = rightParts[i];
                _logger.LogDebug("CreateDiffPreview [Right]: inspecting part '{Part}'", part);
                if ((part.EndsWith(".socpak", StringComparison.OrdinalIgnoreCase) || part.EndsWith(".pak", StringComparison.OrdinalIgnoreCase))
                    && !part.Contains("shadercache_", StringComparison.OrdinalIgnoreCase))
                {
                    var archiveEntry = rightContext.Entries.FirstOrDefault(e => Path.GetFileName(e.Name).Equals(part, StringComparison.OrdinalIgnoreCase));
                    // Log the archive entry name or 'null' if not found
                    _logger.LogDebug("CreateDiffPreview [Right]: found archiveEntry = '{ArchiveEntry}'", archiveEntry?.Name ?? "null");
                    if (archiveEntry != null)
                    {
                        rightContext = P4kFile.FromP4kEntry(rightContext, archiveEntry);
                        // Locate the nested entry by matching the file name directly, ignoring any path prefixes
                        var nestedEntry = rightContext.Entries.FirstOrDefault(e => Path.GetFileName(e.Name)
                            .Equals(fileNode.Name, StringComparison.OrdinalIgnoreCase));
                        // Log the name or 'null' if not found
                        _logger.LogDebug("CreateDiffPreview [Right]: nestedEntry = '{NestedEntry}'", nestedEntry?.Name ?? "null");
                        if (nestedEntry != null)
                            rightEntryToOpen = nestedEntry;
                    }
                    break;
                }
            }
            Stream rightStream;
            try
            {
                _logger.LogDebug("CreateDiffPreview [Right]: opening stream for '{EntryName}'", rightEntryToOpen.Name);
                rightStream = rightContext.OpenStream(rightEntryToOpen);
            }
            catch (Exception ex) when (ex.Message.Contains("Invalid local file header"))
            {
                _logger.LogWarning(ex, "Nested archive read failed, falling back to direct right stream for {FileName}", fileNode.Name);
                rightStream = _rightP4kFile!.OpenStream(fileNode.RightEntry!);
                _logger.LogWarning(ex, "CreateDiffPreview [Right]: OpenStream failed: {Message}, falling back", ex.Message);
            }
            using (rightStream)
            {
                if (StarBreaker.CryXmlB.CryXml.IsCryXmlB(rightStream))
                {
                    if (StarBreaker.CryXmlB.CryXml.TryOpen(rightStream, out var cryXml))
                    {
                        newContent = cryXml.ToString();
                    }
                    else
                    {
                        newContent = "Failed to parse CryXML";
                    }
                }
                else
                {
                    newContent = rightStream.ReadString();
                }
            }
            _logger.LogDebug("CreateDiffPreview [Right]: newContent length {NewContent.Length}", newContent.Length);

            // Check file sizes to provide user feedback for large files
            var oldLineCount = oldContent.Split('\n').Length;
            var newLineCount = newContent.Split('\n').Length;
            
            if (oldLineCount > 20000 || newLineCount > 20000)
            {
                _logger.LogInformation("Large file diff detected - Old: {OldLines} lines, New: {NewLines} lines. Using simplified diff algorithm.", oldLineCount, newLineCount);
            }

            var oldLabel = string.Empty;
            var newLabel = string.Empty;

            // Use .xml extension for CryXML files for better syntax highlighting
            var displayExtension = IsCryXmlFile(fileNode) ? ".xml" : fileExtension;

            _logger.LogInformation("CreateDiffPreview: returning DiffPreviewViewModel for '{FileName}' with extension {DisplayExtension}", fileNode.Name, displayExtension);
            return new DiffPreviewViewModel(oldContent, newContent, oldLabel, newLabel, displayExtension);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create diff preview for file: {FileName}", fileNode.Name);
            return new TextPreviewViewModel($"Failed to create diff preview: {ex.Message}");
        }
    }
    
    private FilePreviewViewModel GetDataCoreFilePreview(DataCoreComparisonFileNode fileNode)
    {
        var fileName = fileNode.Name;
        
        // For modified DataCore files, show a diff view
        if (fileNode.Status == DataCoreComparisonStatus.Modified && 
            fileNode.LeftRecord != null && fileNode.RightRecord != null &&
            fileNode.LeftDatabase != null && fileNode.RightDatabase != null)
        {
            return CreateDataCoreDiffPreview(fileNode);
        }

        // For non-modified files, show the content from appropriate database
        DataCoreDatabase? sourceDatabase = fileNode.Status switch
        {
            DataCoreComparisonStatus.Added => fileNode.RightDatabase,     // File only exists in right DataCore
            DataCoreComparisonStatus.Removed => fileNode.LeftDatabase,   // File only exists in left DataCore
            DataCoreComparisonStatus.Modified => fileNode.RightDatabase, // Show the newer version for non-diff files
            DataCoreComparisonStatus.Unchanged => fileNode.LeftDatabase, // Either database is fine
            _ => fileNode.LeftDatabase
        };

        var sourceRecord = fileNode.Status switch
        {
            DataCoreComparisonStatus.Added => fileNode.RightRecord,
            DataCoreComparisonStatus.Removed => fileNode.LeftRecord,
            DataCoreComparisonStatus.Modified => fileNode.RightRecord,
            DataCoreComparisonStatus.Unchanged => fileNode.LeftRecord ?? fileNode.RightRecord,
            _ => fileNode.LeftRecord ?? fileNode.RightRecord
        };

        if (sourceDatabase == null || sourceRecord == null)
        {
            return new TextPreviewViewModel("DataCore file not available for preview");
        }

        try
        {
            // Create a simple preview showing record information
            var content = GenerateDataCoreRecordPreview(sourceRecord.Value, sourceDatabase);
            var extension = TextFormat == "json" ? ".json" : ".xml";
            
            return new TextPreviewViewModel(content, extension);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create DataCore preview for file: {FileName}", fileName);
            return new TextPreviewViewModel($"Failed to create DataCore preview: {ex.Message}");
        }
    }
    
    private string GenerateDataCoreRecordPreview(DataCoreRecord record, DataCoreDatabase database)
    {
        try
        {
            var fileName = record.GetFileName(database);
            var recordName = record.GetName(database);
            var structTypeName = database.StructDefinitions[record.StructIndex].GetName(database);
            
            // Check if this is a main record that can be extracted
            if (!database.MainRecords.Contains(record.Id))
            {
                _logger.LogDebug("Record {RecordId} is not a main record, showing basic info only", record.Id);
                return GenerateBasicRecordInfo(record, database, fileName, recordName, structTypeName);
            }
            
            // Create a DataForge instance to extract this specific record
            string recordContent;
            if (TextFormat == "json")
            {
                var dataForgeJson = new DataForge<string>(new DataCoreBinaryJson(database));
                recordContent = dataForgeJson.GetFromRecord(record);
            }
            else
            {
                var dataForgeXml = new DataForge<string>(new DataCoreBinaryXml(database));
                recordContent = dataForgeXml.GetFromRecord(record);
            }
            
            if (string.IsNullOrWhiteSpace(recordContent))
            {
                // Fallback to basic info if extraction fails
                return GenerateBasicRecordInfo(record, database, fileName, recordName, structTypeName);
            }
            
            return recordContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract DataCore record content for {RecordId}", record.Id);
            
            // Fallback to basic info on error
            var fileName = record.GetFileName(database);
            var recordName = record.GetName(database);
            var structTypeName = database.StructDefinitions[record.StructIndex].GetName(database);
            return GenerateBasicRecordInfo(record, database, fileName, recordName, structTypeName);
        }
    }
    
    private string GenerateBasicRecordInfo(DataCoreRecord record, DataCoreDatabase database, string fileName, string recordName, string structTypeName)
    {
        var sb = new StringBuilder();
        
        if (TextFormat == "json")
        {
            sb.AppendLine("{");
            sb.AppendLine($"  \"recordId\": \"{record.Id}\",");
            sb.AppendLine($"  \"recordName\": \"{recordName}\",");
            sb.AppendLine($"  \"fileName\": \"{fileName}\",");
            sb.AppendLine($"  \"structType\": \"{structTypeName}\",");
            sb.AppendLine($"  \"structSize\": {record.StructSize}");
            sb.AppendLine("}");
        }
        else
        {
            sb.AppendLine($"<DataCoreRecord>");
            sb.AppendLine($"  <RecordId>{record.Id}</RecordId>");
            sb.AppendLine($"  <RecordName>{recordName}</RecordName>");
            sb.AppendLine($"  <FileName>{fileName}</FileName>");
            sb.AppendLine($"  <StructType>{structTypeName}</StructType>");
            sb.AppendLine($"  <StructSize>{record.StructSize}</StructSize>");
            sb.AppendLine($"</DataCoreRecord>");
        }
        
        return sb.ToString();
    }

    private FilePreviewViewModel CreateDataCoreDiffPreview(DataCoreComparisonFileNode fileNode)
    {
        if (fileNode.LeftDatabase == null || fileNode.RightDatabase == null || 
            fileNode.LeftRecord == null || fileNode.RightRecord == null)
        {
            return new TextPreviewViewModel("Unable to create diff - missing DataCore data");
        }

        try
        {
            // Check if both records are main records that can be extracted
            var leftIsMainRecord = fileNode.LeftDatabase.MainRecords.Contains(fileNode.LeftRecord.Value.Id);
            var rightIsMainRecord = fileNode.RightDatabase.MainRecords.Contains(fileNode.RightRecord.Value.Id);
            
            string oldContent, newContent;
            
            // Extract full XML/JSON content for both records if they're main records
            if (leftIsMainRecord && rightIsMainRecord)
            {
                if (TextFormat == "json")
                {
                    var leftDataForge = new DataForge<string>(new DataCoreBinaryJson(fileNode.LeftDatabase));
                    var rightDataForge = new DataForge<string>(new DataCoreBinaryJson(fileNode.RightDatabase));
                    
                    oldContent = leftDataForge.GetFromRecord(fileNode.LeftRecord.Value);
                    newContent = rightDataForge.GetFromRecord(fileNode.RightRecord.Value);
                }
                else
                {
                    var leftDataForge = new DataForge<string>(new DataCoreBinaryXml(fileNode.LeftDatabase));
                    var rightDataForge = new DataForge<string>(new DataCoreBinaryXml(fileNode.RightDatabase));
                    
                    oldContent = leftDataForge.GetFromRecord(fileNode.LeftRecord.Value);
                    newContent = rightDataForge.GetFromRecord(fileNode.RightRecord.Value);
                }
            }
            else
            {
                // One or both records are not main records, use basic info generation
                _logger.LogDebug("One or both records are not main records (Left: {LeftIsMain}, Right: {RightIsMain}), using basic info", leftIsMainRecord, rightIsMainRecord);
                oldContent = GenerateDataCoreRecordPreview(fileNode.LeftRecord.Value, fileNode.LeftDatabase);
                newContent = GenerateDataCoreRecordPreview(fileNode.RightRecord.Value, fileNode.RightDatabase);
            }
            
            // Fallback to basic preview if extraction fails
            if (string.IsNullOrWhiteSpace(oldContent))
            {
                oldContent = GenerateDataCoreRecordPreview(fileNode.LeftRecord.Value, fileNode.LeftDatabase);
            }
            
            if (string.IsNullOrWhiteSpace(newContent))
            {
                newContent = GenerateDataCoreRecordPreview(fileNode.RightRecord.Value, fileNode.RightDatabase);
            }

            // Create labels for the diff
            var oldLabel = $"Left P4K";
            var newLabel = $"Right P4K";

            var displayExtension = TextFormat == "json" ? ".json" : ".xml";

            return new DiffPreviewViewModel(oldContent, newContent, oldLabel, newLabel, displayExtension);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create DataCore diff preview for file: {FileName}", fileNode.Name);
            return new TextPreviewViewModel($"Failed to create DataCore diff preview: {ex.Message}");
        }
    }

    private void LoadSettings()
    {
        try
        {
            if (File.Exists(Constants.SettingsFile))
            {
                var json = File.ReadAllText(Constants.SettingsFile);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                
                // First, load the saved game folder if it exists and is valid
                if (!string.IsNullOrWhiteSpace(settings?.DiffGameFolder) && Directory.Exists(settings.DiffGameFolder))
                {
                    GameFolder = settings.DiffGameFolder;
                }

                // Load output directory
                if (!string.IsNullOrWhiteSpace(settings?.DiffOutputDirectory))
                {
                    OutputDirectory = settings.DiffOutputDirectory;
                }
                else
                {
                    var defaultOutputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "StarCitizen_Diff");
                    OutputDirectory = defaultOutputPath;
                }

                // Load text format
                if (!string.IsNullOrWhiteSpace(settings?.TextFormat))
                {
                    TextFormat = settings.TextFormat;
                }
                else
                {
                    TextFormat = "xml"; // default
                }

                // Load P4K output directory
                if (!string.IsNullOrWhiteSpace(settings?.P4kOutputDirectory))
                {
                    P4kOutputDirectory = settings.P4kOutputDirectory;
                }
                else
                {
                    var defaultP4kOutputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "StarCitizen_P4K_Output");
                    P4kOutputDirectory = defaultP4kOutputPath;
                }

                // Now determine and set the selected channel based on saved setting or current GameFolder
                string channelToSelect = "LIVE"; // default
                
                if (!string.IsNullOrWhiteSpace(settings?.SelectedChannel))
                {
                    channelToSelect = settings.SelectedChannel;
                }
                else if (!string.IsNullOrWhiteSpace(GameFolder))
                {
                    // Try to derive channel from current GameFolder
                    var folderName = Path.GetFileName(GameFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                    if (folderName == "LIVE" || folderName == "PTU" || folderName == "EPTU" || folderName == "TECH-PREVIEW")
                    {
                        channelToSelect = folderName;
                    }
                }
                
                // Update channel selection (this will also update GameFolder if needed)
                UpdateChannelSelection(channelToSelect);

                _logger.LogInformation("Loaded diff settings - Channel: {Channel}, Game: {GameFolder}, Output: {OutputDirectory}, Format: {Format}", SelectedChannel, GameFolder, OutputDirectory, TextFormat);
            }
            else
            {
                // No settings file exists, set defaults
                var defaultOutputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "StarCitizen_Diff");
                OutputDirectory = defaultOutputPath;
                TextFormat = "xml";
                
                var defaultP4kOutputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "StarCitizen_P4K_Output");
                P4kOutputDirectory = defaultP4kOutputPath;
                
                UpdateChannelSelection("LIVE");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load diff settings");
            
            // Fallback to defaults
            var defaultOutputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "StarCitizen_Diff");
            OutputDirectory = defaultOutputPath;
            TextFormat = "xml";
            
            var defaultP4kOutputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "StarCitizen_P4K_Output");
            P4kOutputDirectory = defaultP4kOutputPath;
            
            UpdateChannelSelection("LIVE");
        }
    }

    private void SaveSettings()
    {
        try
        {
            var folder = Path.GetDirectoryName(Constants.SettingsFile)!;
            Directory.CreateDirectory(folder);
            
            // Load existing settings first to preserve other settings
            AppSettings settings = new();
            if (File.Exists(Constants.SettingsFile))
            {
                var existingJson = File.ReadAllText(Constants.SettingsFile);
                settings = JsonSerializer.Deserialize<AppSettings>(existingJson) ?? new AppSettings();
            }
            
            // Update diff-specific settings
            settings.SelectedChannel = SelectedChannel;
            settings.DiffGameFolder = GameFolder;
            settings.DiffOutputDirectory = OutputDirectory;
            settings.TextFormat = TextFormat;
            settings.P4kOutputDirectory = P4kOutputDirectory;
            
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Constants.SettingsFile, json);
            _logger.LogInformation("Saved diff settings - Channel: {Channel}, Game: {GameFolder}, Output: {OutputDirectory}, Format: {Format}", SelectedChannel, GameFolder, OutputDirectory, TextFormat);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save diff settings");
        }
    }

    partial void OnGameFolderChanged(string value)
    {
        SaveSettings();
    }

    partial void OnOutputDirectoryChanged(string value)
    {
        SaveSettings();
    }

    partial void OnSelectedChannelChanged(string value)
    {
        SaveSettings();
    }

    partial void OnTextFormatChanged(string value)
    {
        SaveSettings();
    }

    partial void OnP4kOutputDirectoryChanged(string value)
    {
        SaveSettings();
        OnPropertyChanged(nameof(CanCreateReport));
        OnPropertyChanged(nameof(CanCreateP4kReport));
        OnPropertyChanged(nameof(CanExtractNewDdsFiles));
    }

    private string GetBaseInstallationPath()
    {
        // First, check if GameFolder appears to be a base installation folder
        if (!string.IsNullOrWhiteSpace(GameFolder) && Directory.Exists(GameFolder))
        {
            var gamefolderName = Path.GetFileName(GameFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            
            // If GameFolder ends with a known channel name, use its parent as base path
            if (gamefolderName == "LIVE" || gamefolderName == "PTU" || gamefolderName == "EPTU")
            {
                var basePath = Path.GetDirectoryName(GameFolder);
                if (!string.IsNullOrWhiteSpace(basePath) && Directory.Exists(basePath))
                {
                    _logger.LogInformation("Derived base path from GameFolder channel: {BasePath}", basePath);
                    return basePath;
                }
            }
            // If GameFolder appears to be a base installation folder (contains channel subdirectories)
            else if (Directory.Exists(Path.Combine(GameFolder, "LIVE")) || 
                     Directory.Exists(Path.Combine(GameFolder, "PTU")) || 
                     Directory.Exists(Path.Combine(GameFolder, "EPTU")) ||
                     Directory.Exists(Path.Combine(GameFolder, "TECH-PREVIEW")))
            {
                _logger.LogInformation("Using GameFolder as base path (contains channels): {BasePath}", GameFolder);
                return GameFolder;
            }
            // If GameFolder looks like a StarCitizen installation folder by name
            else if (gamefolderName.Equals("StarCitizen", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Using GameFolder as base path (StarCitizen folder): {BasePath}", GameFolder);
                return GameFolder;
            }
        }

        // Try to get the custom installation folder from settings
        try
        {
            if (File.Exists(Constants.SettingsFile))
            {
                var json = File.ReadAllText(Constants.SettingsFile);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (!string.IsNullOrWhiteSpace(settings?.CustomInstallFolder) && Directory.Exists(settings.CustomInstallFolder))
                {
                    _logger.LogInformation("Using custom installation folder from settings: {Path}", settings.CustomInstallFolder);
                    return settings.CustomInstallFolder;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load custom installation folder");
        }
        
        // Fallback to default
        _logger.LogInformation("Using default StarCitizen folder: {Path}", Constants.DefaultStarCitizenFolder);
        return Constants.DefaultStarCitizenFolder;
    }

    private void UpdateChannelSelection(string channel)
    {
        SelectedChannel = channel;
        IsLiveSelected = channel == "LIVE";
        IsPtuSelected = channel == "PTU";
        IsEptuSelected = channel == "EPTU";
        IsTechPreviewSelected = channel == "TECH-PREVIEW";
        
        var basePath = GetBaseInstallationPath();
        var channelPath = Path.Combine(basePath, channel);
        
        if (Directory.Exists(channelPath))
        {
            // Do not override GameFolder; keep user-selected base path stable
            AddLogMessage($"Selected {channel} channel: {channelPath}");
            // Set default output repo paths for TECH-PREVIEW
            if (channel == "TECH-PREVIEW")
            {
                var previewRepo = @"C:\\Development\\StarCitizen\\StarCitizenDiffTechPreview";
                if (Directory.Exists(previewRepo))
                {
                    OutputDirectory = previewRepo;
                    P4kOutputDirectory = previewRepo;
                }
                else
                {
                    AddLogMessage($"TECH-PREVIEW output folder not found at {previewRepo}. Using current settings.");
                }
            }
            else if (channel == "LIVE" || channel == "PTU" || channel == "EPTU")
            {
                var defaultRepo = @"C:\\Development\\StarCitizen\\StarCitizenDiff";
                if (Directory.Exists(defaultRepo))
                {
                    OutputDirectory = defaultRepo;
                    P4kOutputDirectory = defaultRepo;
                }
                else
                {
                    AddLogMessage($"Default repository folder not found at {defaultRepo}. Using current settings.");
                }
            }
        }
        else
        {
            AddLogMessage($"{channel} channel not found at: {channelPath}");
        }
    }

    [RelayCommand]
    public void SelectLiveChannel()
    {
        UpdateChannelSelection("LIVE");
    }

    [RelayCommand]
    public void SelectPtuChannel()
    {
        UpdateChannelSelection("PTU");
    }

    [RelayCommand]
    public void SelectEptuChannel()
    {
        UpdateChannelSelection("EPTU");
    }

    [RelayCommand]
    public void SelectTechPreviewChannel()
    {
        UpdateChannelSelection("TECH-PREVIEW");
    }
    
    [RelayCommand]
    public void ToggleP4kComparisonMode()
    {
        _logger.LogInformation("ToggleP4kComparisonMode command executed. Current mode: {Current}", IsP4kComparisonMode);
        IsP4kComparisonMode = !IsP4kComparisonMode;
        if (IsP4kComparisonMode)
        {
            IsDataCoreComparisonMode = false; // Ensure only one comparison mode is active
        }
        _logger.LogInformation("Switched to {Mode} mode. New value: {Value}", IsP4kComparisonMode ? "P4K Comparison" : "Diff Tool", IsP4kComparisonMode);
        // Reset preview when switching modes
        Preview = null;
        ShowNoSelectionMessage = true;
        // Clear DataCore selection when switching to P4K mode
        SelectedDataCoreFiles = new List<IDataCoreComparisonNode>();
    }
    
    [RelayCommand]
    public void ToggleDataCoreComparisonMode()
    {
        _logger.LogInformation("ToggleDataCoreComparisonMode command executed. Current mode: {Current}", IsDataCoreComparisonMode);
        IsDataCoreComparisonMode = !IsDataCoreComparisonMode;
        if (IsDataCoreComparisonMode)
        {
            IsP4kComparisonMode = false; // Ensure only one comparison mode is active
        }
        _logger.LogInformation("Switched to {Mode} mode. New value: {Value}", IsDataCoreComparisonMode ? "DataCore Comparison" : "Diff Tool", IsDataCoreComparisonMode);
        // Reset preview when switching modes
        Preview = null;
        ShowNoSelectionMessage = true;
        // Clear P4K selection when switching to DataCore mode
        SelectedP4kFiles = new List<IP4kComparisonNode>();
    }

    [RelayCommand]
    public void ToggleShowOnlyChangedFiles()
    {
        ShowOnlyChangedFiles = !ShowOnlyChangedFiles;
        RefreshComparisonTree();
        RefreshDataCoreComparisonTree();
        _logger.LogInformation("Show only changed files: {ShowOnlyChanged}", ShowOnlyChangedFiles);
    }

    private void RefreshComparisonTree()
    {
        if (_comparisonRoot == null || ComparisonSource == null) return;

        var filteredItems = _comparisonRoot.GetFilteredComparisonChildren(ShowOnlyChangedFiles);
        ComparisonSource.Items = filteredItems.OrderBy(n => n is not P4kComparisonDirectoryNode)
            .ThenBy(n => n.GetComparisonName(), StringComparer.OrdinalIgnoreCase).ToArray();
    }
    
    private void RefreshDataCoreComparisonTree()
    {
        if (_dataCoreComparisonRoot == null || DataCoreComparisonSource == null) return;

        var filteredItems = _dataCoreComparisonRoot.GetFilteredComparisonChildren(ShowOnlyChangedFiles);
        DataCoreComparisonSource.Items = filteredItems.OrderBy(n => n is not DataCoreComparisonDirectoryNode)
            .ThenBy(n => n.GetComparisonName(), StringComparer.OrdinalIgnoreCase).ToArray();
    }
    
    [RelayCommand]
    public async Task SelectLeftP4k()
    {
        var topLevel = TopLevel.GetTopLevel(Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop 
            ? desktop.MainWindow : null);

            if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
            Title = "Select Left P4K File",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("P4K Files") { Patterns = new[] { "*.p4k" } },
                new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
                }
        });

            if (files.Count > 0)
            {
            LeftP4kPath = files[0].Path.LocalPath;
            _logger.LogInformation("Selected left P4K: {Path}", LeftP4kPath);
        }
    }

    [RelayCommand]
    public async Task SelectRightP4k()
    {
        var topLevel = TopLevel.GetTopLevel(Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop 
            ? desktop.MainWindow : null);

            if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
            Title = "Select Right P4K File",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("P4K Files") { Patterns = new[] { "*.p4k" } },
                new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
                }
        });

            if (files.Count > 0)
            {
            RightP4kPath = files[0].Path.LocalPath;
            _logger.LogInformation("Selected right P4K: {Path}", RightP4kPath);
        }
    }

    [RelayCommand]
    public async Task CompareP4ks()
    {
        if (string.IsNullOrWhiteSpace(LeftP4kPath) || string.IsNullOrWhiteSpace(RightP4kPath))
        {
            _logger.LogWarning("Both P4K files must be selected for comparison");
            return;
        }

        if (!File.Exists(LeftP4kPath) || !File.Exists(RightP4kPath))
        {
            _logger.LogError("One or both P4K files do not exist");
            return;
        }

        IsComparing = true;
        ComparisonStatus = "Loading P4K files...";
        
        try
        {
            await Task.Run(() =>
            {
                Dispatcher.UIThread.Post(() => ComparisonStatus = "Loading left P4K...");
                var leftP4k = P4kFile.FromFile(LeftP4kPath);
                
                Dispatcher.UIThread.Post(() => ComparisonStatus = "Loading right P4K...");
                var rightP4k = P4kFile.FromFile(RightP4kPath);
                
                Dispatcher.UIThread.Post(() => ComparisonStatus = "Comparing P4K files...");
                var progress = new Progress<double>(p => 
                    Dispatcher.UIThread.Post(() => ComparisonStatus = $"Comparing P4K files... {p:P0}"));
                
                var comparisonRoot = P4kComparison.Compare(leftP4k, rightP4k, progress);
                
                Dispatcher.UIThread.Post(() =>
                {
                    // Store P4K files and comparison root for preview and filtering
                    _leftP4kFile = leftP4k;
                    _rightP4kFile = rightP4k;
                    _comparisonRoot = comparisonRoot;
                    
                    RefreshComparisonTree();
                    
                    // Notify that CanCreateP4kReport and CanExtractNewDdsFiles have changed
                    OnPropertyChanged(nameof(CanCreateP4kReport));
                    OnPropertyChanged(nameof(CanExtractNewDdsFiles));
                    OnPropertyChanged(nameof(CanExtractNewAudioFiles));
                    
                    var stats = P4kComparison.AnalyzeComparison(comparisonRoot);
                    ComparisonStatus = $"Comparison complete! Added: {stats.AddedFiles}, Removed: {stats.RemovedFiles}, Modified: {stats.ModifiedFiles}";
                    
                    _logger.LogInformation("P4K comparison completed - Total: {Total}, Added: {Added}, Removed: {Removed}, Modified: {Modified}, Unchanged: {Unchanged}",
                        stats.TotalFiles, stats.AddedFiles, stats.RemovedFiles, stats.ModifiedFiles, stats.UnchangedFiles);
                });
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compare P4K files");
            ComparisonStatus = $"Comparison failed: {ex.Message}";
        }
        finally
        {
            IsComparing = false;
        }
    }

    [RelayCommand]
    public async Task SelectLeftDataCoreP4k()
    {
        var topLevel = TopLevel.GetTopLevel(Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop 
            ? desktop.MainWindow : null);

            if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
            Title = "Select Left P4K File (for DataCore)",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("P4K Files") { Patterns = new[] { "*.p4k" } },
                new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
                }
        });

            if (files.Count > 0)
            {
            LeftDataCoreP4kPath = files[0].Path.LocalPath;
            _logger.LogInformation("Selected left P4K for DataCore comparison: {Path}", LeftDataCoreP4kPath);
        }
    }

    [RelayCommand]
    public async Task SelectRightDataCoreP4k()
    {
        var topLevel = TopLevel.GetTopLevel(Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop 
            ? desktop.MainWindow : null);

        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Right P4K File (for DataCore)",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("P4K Files") { Patterns = new[] { "*.p4k" } },
                new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
            }
        });

        if (files.Count > 0)
        {
            RightDataCoreP4kPath = files[0].Path.LocalPath;
            _logger.LogInformation("Selected right P4K for DataCore comparison: {Path}", RightDataCoreP4kPath);
        }
    }

    [RelayCommand]
    public async Task CompareDataCores()
    {
        if (string.IsNullOrWhiteSpace(LeftDataCoreP4kPath) || string.IsNullOrWhiteSpace(RightDataCoreP4kPath))
        {
            _logger.LogWarning("Both P4K files must be selected for DataCore comparison");
            return;
        }

        if (!File.Exists(LeftDataCoreP4kPath) || !File.Exists(RightDataCoreP4kPath))
        {
            _logger.LogError("One or both P4K files do not exist");
            return;
        }

        IsComparing = true;
        ComparisonStatus = "Loading P4K files...";
        
        try
        {
            await Task.Run(() =>
            {
                // Load left DataCore from P4K
                Dispatcher.UIThread.Post(() => ComparisonStatus = "Loading left P4K and extracting DataCore...");
                var leftDataCore = LoadDataCoreFromP4k(LeftDataCoreP4kPath);
                if (leftDataCore == null)
                {
                    throw new InvalidOperationException($"DataCore not found in left P4K: {LeftDataCoreP4kPath}");
                }
                
                // Load right DataCore from P4K
                Dispatcher.UIThread.Post(() => ComparisonStatus = "Loading right P4K and extracting DataCore...");
                var rightDataCore = LoadDataCoreFromP4k(RightDataCoreP4kPath);
                if (rightDataCore == null)
                {
                    throw new InvalidOperationException($"DataCore not found in right P4K: {RightDataCoreP4kPath}");
                }
                
                Dispatcher.UIThread.Post(() => ComparisonStatus = "Comparing DataCore files...");
                var progress = new Progress<double>(p => 
                    Dispatcher.UIThread.Post(() => ComparisonStatus = $"Comparing DataCore files... {p:P0}"));
                
                var comparisonRoot = DataCoreComparison.Compare(leftDataCore, rightDataCore, progress);
        
        Dispatcher.UIThread.Post(() =>
        {
                    // Store DataCore databases and comparison root for preview and filtering
                    _leftDataCoreDatabase = leftDataCore;
                    _rightDataCoreDatabase = rightDataCore;
                    _dataCoreComparisonRoot = comparisonRoot;
                    
                    RefreshDataCoreComparisonTree();
                    
                    // Notify that CanCreateReport has changed
                    OnPropertyChanged(nameof(CanCreateReport));
                    
                    var stats = DataCoreComparison.AnalyzeComparison(comparisonRoot);
                    ComparisonStatus = $"Comparison complete! Added: {stats.AddedFiles}, Removed: {stats.RemovedFiles}, Modified: {stats.ModifiedFiles}";
                    
                    _logger.LogInformation("DataCore comparison completed - Total: {Total}, Added: {Added}, Removed: {Removed}, Modified: {Modified}, Unchanged: {Unchanged}",
                        stats.TotalFiles, stats.AddedFiles, stats.RemovedFiles, stats.ModifiedFiles, stats.UnchangedFiles);
                });
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compare DataCore files");
            ComparisonStatus = $"Comparison failed: {ex.Message}";
        }
        finally
        {
            IsComparing = false;
        }
    }

    private DataCoreDatabase? LoadDataCoreFromP4k(string p4kPath)
    {
        try
        {
            var p4kFileSystem = new P4kFileSystem(P4kFile.FromFile(p4kPath));
            
            // Try to find DataCore file in known paths
            Stream? dcbStream = null;
            foreach (var file in DataCoreUtils.KnownPaths)
            {
                if (!p4kFileSystem.FileExists(file)) continue;
                dcbStream = p4kFileSystem.OpenRead(file);
                _logger.LogInformation("Found DataCore at path: {Path} in P4K: {P4kPath}", file, p4kPath);
                break;
            }

            if (dcbStream == null)
            {
                _logger.LogError("DataCore not found in any known paths in P4K: {P4kPath}", p4kPath);
                return null;
            }

            return new DataCoreDatabase(dcbStream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load DataCore from P4K: {P4kPath}", p4kPath);
            return null;
        }
    }

    /// <summary>
    /// Indicates whether a DataCore report can be created (DataCore comparison has been completed and output directory is configured)
    /// </summary>
    public bool CanCreateReport => _dataCoreComparisonRoot != null && _leftDataCoreDatabase != null && _rightDataCoreDatabase != null && !string.IsNullOrWhiteSpace(P4kOutputDirectory);

    /// <summary>
    /// Indicates whether a P4K report can be created (P4K comparison has been completed)
    /// </summary>
    public bool CanCreateP4kReport => _comparisonRoot != null && _leftP4kFile != null && _rightP4kFile != null;

    /// <summary>
    /// Indicates whether new DDS files can be extracted (P4K comparison has been completed and there are added DDS files)
    /// </summary>
    public bool CanExtractNewDdsFiles => _comparisonRoot != null && _rightP4kFile != null && 
        _comparisonRoot.GetAllFiles().Any(f => (f.Status == P4kComparisonStatus.Added || f.Status == P4kComparisonStatus.Modified) && 
            Path.GetFileName(f.FullPath).Contains(".dds", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Indicates whether new audio files can be extracted (P4K comparison has been completed and there are added audio files)
    /// </summary>
    public bool CanExtractNewAudioFiles => _comparisonRoot != null && _rightP4kFile != null && 
        _comparisonRoot.GetAllFiles().Any(f => f.Status == P4kComparisonStatus.Added && 
            IsAudioFile(f.FullPath));

    /// <summary>
    /// Indicates whether selected P4K files can be extracted (files are selected)
    /// </summary>
    public bool CanExtractSelectedP4kFiles => SelectedP4kFiles.Any(f => f is P4kComparisonFileNode) && 
        !string.IsNullOrWhiteSpace(P4kOutputDirectory);

    /// <summary>
    /// Indicates whether selected DataCore files can be extracted (files are selected)
    /// </summary>
    public bool CanExtractSelectedDataCoreFiles => SelectedDataCoreFiles.Any(f => f is DataCoreComparisonFileNode) && 
        !string.IsNullOrWhiteSpace(P4kOutputDirectory);

    private static bool IsAudioFile(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        return fileName.EndsWith(".wem", StringComparison.OrdinalIgnoreCase);
    }

    [RelayCommand]
    public async Task CreateReport()
    {
        if (!CanCreateReport)
        {
            _logger.LogWarning("Cannot create report - no comparison data available");
            return;
        }

        try
        {
            if (string.IsNullOrWhiteSpace(P4kOutputDirectory))
            {
                _logger.LogWarning("P4K output directory not configured");
                ComparisonStatus = "Please configure output directory first";
            return;
        }

            Directory.CreateDirectory(P4kOutputDirectory);
            
            var leftVersion = SanitizeForFilename(ExtractVersionFromManifest(LeftDataCoreP4kPath));
            var rightVersion = SanitizeForFilename(ExtractVersionFromManifest(RightDataCoreP4kPath));
            var reportFileName = $"DataCore_Comparison_{leftVersion}_to_{rightVersion}.md";
            var reportPath = Path.Combine(P4kOutputDirectory, reportFileName);

            IsComparing = true;
            ComparisonStatus = "Generating report...";

            await Task.Run(async () =>
            {
                try
                {
                    var report = await GenerateComparisonReport();
                    
                    await File.WriteAllTextAsync(reportPath, report, Encoding.UTF8);
                    
                    Dispatcher.UIThread.Post(() =>
                    {
                        ComparisonStatus = $"DataCore report saved successfully to {reportFileName}";
                        _logger.LogInformation("DataCore comparison report saved to: {FilePath}", reportPath);
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate or save report");
                    Dispatcher.UIThread.Post(() => ComparisonStatus = $"Failed to save report: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating report");
            ComparisonStatus = $"Error creating report: {ex.Message}";
        }
        finally
        {
            IsComparing = false;
        }
    }

    private async Task<string> GenerateComparisonReport()
    {
        if (_dataCoreComparisonRoot == null || _leftDataCoreDatabase == null || _rightDataCoreDatabase == null)
            throw new InvalidOperationException("No comparison data available");

        var report = new StringBuilder();
        var stats = DataCoreComparison.AnalyzeComparison(_dataCoreComparisonRoot);

        // Extract localization data for resolving keys
        var leftLocalization = await ExtractLocalizationData(LeftDataCoreP4kPath);
        var rightLocalization = await ExtractLocalizationData(RightDataCoreP4kPath);
        
        // Merge localization data (prefer right/newer version)
        var mergedLocalization = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (leftLocalization != null)
        {
            foreach (var kvp in leftLocalization)
                mergedLocalization[kvp.Key] = kvp.Value;
        }
        if (rightLocalization != null)
        {
            foreach (var kvp in rightLocalization)
                mergedLocalization[kvp.Key] = kvp.Value;
        }
        
        // Store for use in extraction methods
        _currentLocalizationData = mergedLocalization;
        
        _logger.LogDebug("Available vehicle keys: {VehicleKeys}", 
            string.Join(", ", mergedLocalization.Keys.Where(k => k.StartsWith("vehicle_")).Take(10)));

        // Header
        var leftVersion = ExtractVersionFromManifest(LeftDataCoreP4kPath);
        var rightVersion = ExtractVersionFromManifest(RightDataCoreP4kPath);
        
        report.AppendLine($"# Post (Datamine) | New {rightVersion} datamines");
        report.AppendLine();
        report.AppendLine($"**Version:** {leftVersion} → {rightVersion}");
        report.AppendLine($"**Generated:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine();

        // Summary
        report.AppendLine("## Summary");
        report.AppendLine($"- **Total Files:** {stats.TotalFiles}");
        report.AppendLine($"- **Added:** {stats.AddedFiles}");
        report.AppendLine($"- **Removed:** {stats.RemovedFiles}");
        report.AppendLine($"- **Modified:** {stats.ModifiedFiles}");
        report.AppendLine($"- **Unchanged:** {stats.UnchangedFiles}");
        report.AppendLine();
        
        // === DISCORD BREAK === (Copy sections below separately for Discord's 4000 char limit)
        report.AppendLine("---");
        report.AppendLine();

        // New Ships Section
        GenerateNewShipsSection(report);
        
        report.AppendLine("---");
        report.AppendLine();
        
        // New Weapons Section
        GenerateNewWeaponsSection(report);
        
        report.AppendLine("---");
        report.AppendLine();
        
        // New Components Section
        GenerateNewComponentsSection(report);
        
        report.AppendLine("---");
        report.AppendLine();
        
        // New Paints Section
        GenerateNewPaintsSection(report);
        
        report.AppendLine("---");
        report.AppendLine();
        
        // Changed Files Section
        GenerateChangedFilesSection(report);
        
        report.AppendLine("---");
        report.AppendLine();
        
        // Localization Changes Section
        await GenerateLocalizationChangesSection(report);

        return report.ToString();
    }

    private void GenerateNewShipsSection(StringBuilder report)
    {
        if (_dataCoreComparisonRoot == null || _rightDataCoreDatabase == null) return;

        var allFiles = _dataCoreComparisonRoot.GetAllFiles().ToArray();
        
        // Find added files in the spaceships directory
        var newShips = allFiles
            .Where(f => f.Status == DataCoreComparisonStatus.Added)
            .Where(f => f.FullPath.StartsWith("libs/foundry/records/entities/spaceships", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (newShips.Length > 0)
        {
            var breakHelper = new DiscordBreakHelper(report);
            breakHelper.AppendLine("## New Ships Added");
            breakHelper.AppendLine();
            
            foreach (var ship in newShips.OrderBy(s => s.FullPath))
            {
                var shipInfo = ExtractShipInfo(ship, _rightDataCoreDatabase);
                if (shipInfo != null)
                {
                    breakHelper.AppendLine($"- **{shipInfo.DisplayName}**");
                    if (!string.IsNullOrEmpty(shipInfo.Description))
                    {
                        breakHelper.AppendLine($"  - {shipInfo.Description}");
                    }
                    if (!string.IsNullOrEmpty(shipInfo.Manufacturer))
                    {
                        breakHelper.AppendLine($"  - Manufacturer: {shipInfo.Manufacturer}");
                    }
                }
                else
                {
                    var shipName = Path.GetFileNameWithoutExtension(ship.FullPath);
                    var displayName = CleanDisplayName(shipName);
                    breakHelper.AppendLine($"- **{displayName}**");
                }
            }
            breakHelper.AppendLine();
        }
    }

    private void GenerateNewWeaponsSection(StringBuilder report)
    {
        if (_dataCoreComparisonRoot == null || _rightDataCoreDatabase == null) return;

        var allFiles = _dataCoreComparisonRoot.GetAllFiles().ToArray();
        
        // Find added files in the weapons and related directories
        var weaponDirectories = new[]
        {
            "libs/foundry/records/entities/scitem/ships/weapons",
            "libs/foundry/records/entities/scitem/ships/missile_racks",
            "libs/foundry/records/entities/scitem/ships/weapon_mounts",
            "libs/foundry/records/entities/scitem/ships/turret"
        };
        
        var newWeapons = allFiles
            .Where(f => f.Status == DataCoreComparisonStatus.Added)
            .Where(f => weaponDirectories.Any(dir => f.FullPath.StartsWith(dir, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        if (newWeapons.Length > 0)
        {
            var breakHelper = new DiscordBreakHelper(report);
            breakHelper.AppendLine("## New Weapons & Turrets Added");
            breakHelper.AppendLine();
            
            // Group weapons by type
            var weapons = newWeapons.Where(w => w.FullPath.StartsWith("libs/foundry/records/entities/scitem/ships/weapons", StringComparison.OrdinalIgnoreCase) && !w.FullPath.Contains("/parts/"));
            var weaponParts = newWeapons.Where(w => w.FullPath.StartsWith("libs/foundry/records/entities/scitem/ships/weapons", StringComparison.OrdinalIgnoreCase) && w.FullPath.Contains("/parts/"));
            var missileRacks = newWeapons.Where(w => w.FullPath.StartsWith("libs/foundry/records/entities/scitem/ships/missile_racks", StringComparison.OrdinalIgnoreCase));
            var weaponMounts = newWeapons.Where(w => w.FullPath.StartsWith("libs/foundry/records/entities/scitem/ships/weapon_mounts", StringComparison.OrdinalIgnoreCase));
            var turrets = newWeapons.Where(w => w.FullPath.StartsWith("libs/foundry/records/entities/scitem/ships/turret", StringComparison.OrdinalIgnoreCase));
            
            if (weapons.Any())
            {
                breakHelper.AppendLine("### Ship Weapons");
                foreach (var weapon in weapons.OrderBy(w => w.FullPath))
                {
                    var weaponInfo = ExtractWeaponInfo(weapon, _rightDataCoreDatabase);
                    if (weaponInfo != null)
                    {
                        var sizeStr = weaponInfo.Size > 0 ? $"S{weaponInfo.Size} " : "";
                        breakHelper.AppendLine($"- **{sizeStr}{weaponInfo.DisplayName}**");
                        if (!string.IsNullOrEmpty(weaponInfo.Description))
                        {
                            breakHelper.AppendLine($"  - {weaponInfo.Description}");
                        }
                    }
                    else
                    {
                        var weaponName = Path.GetFileNameWithoutExtension(weapon.FullPath);
                        var displayName = CleanDisplayName(weaponName);
                        breakHelper.AppendLine($"- **{displayName}**");
                    }
                }
                breakHelper.AppendLine();
            }
            
            if (missileRacks.Any())
            {
                breakHelper.AppendLine("### Missile Racks");
                foreach (var missileRack in missileRacks.OrderBy(m => m.FullPath))
                {
                    var rackInfo = ExtractWeaponInfo(missileRack, _rightDataCoreDatabase);
                    if (rackInfo != null)
                    {
                        var sizeStr = rackInfo.Size > 0 ? $"S{rackInfo.Size} " : "";
                        breakHelper.AppendLine($"- **{sizeStr}{rackInfo.DisplayName}**");
                        if (!string.IsNullOrEmpty(rackInfo.Description))
                        {
                            breakHelper.AppendLine($"  - {rackInfo.Description}");
                        }
                    }
                    else
                    {
                        var rackName = Path.GetFileNameWithoutExtension(missileRack.FullPath);
                        var displayName = CleanDisplayName(rackName);
                        breakHelper.AppendLine($"- **{displayName}**");
                    }
                }
                breakHelper.AppendLine();
            }
            
            if (turrets.Any())
            {
                breakHelper.AppendLine("### Turrets");
                foreach (var turret in turrets.OrderBy(t => t.FullPath))
                {
                    var turretInfo = ExtractWeaponInfo(turret, _rightDataCoreDatabase);
                    if (turretInfo != null)
                    {
                        var sizeStr = turretInfo.Size > 0 ? $"S{turretInfo.Size} " : "";
                        breakHelper.AppendLine($"- **{sizeStr}{turretInfo.DisplayName}**");
                        if (!string.IsNullOrEmpty(turretInfo.Description))
                        {
                            breakHelper.AppendLine($"  - {turretInfo.Description}");
                        }
                    }
                    else
                    {
                        var turretName = Path.GetFileNameWithoutExtension(turret.FullPath);
                        var displayName = CleanDisplayName(turretName);
                        breakHelper.AppendLine($"- **{displayName}**");
                    }
                }
                breakHelper.AppendLine();
            }
            
            if (weaponMounts.Any())
            {
                breakHelper.AppendLine("### Weapon Mounts");
                foreach (var mount in weaponMounts.OrderBy(w => w.FullPath))
                {
                    var mountInfo = ExtractWeaponInfo(mount, _rightDataCoreDatabase);
                    if (mountInfo != null)
                    {
                        var sizeStr = mountInfo.Size > 0 ? $"S{mountInfo.Size} " : "";
                        breakHelper.AppendLine($"- **{sizeStr}{mountInfo.DisplayName}**");
                        if (!string.IsNullOrEmpty(mountInfo.Description))
                        {
                            breakHelper.AppendLine($"  - {mountInfo.Description}");
                        }
                    }
                    else
                    {
                        var mountName = Path.GetFileNameWithoutExtension(mount.FullPath);
                        var displayName = CleanDisplayName(mountName);
                        breakHelper.AppendLine($"- **{displayName}**");
                    }
                }
                breakHelper.AppendLine();
            }
            
            if (weaponParts.Any())
            {
                breakHelper.AppendLine("### Weapon Parts");
                foreach (var part in weaponParts.OrderBy(w => w.FullPath))
                {
                    var partName = Path.GetFileNameWithoutExtension(part.FullPath);
                    var displayName = CleanDisplayName(partName);
                    breakHelper.AppendLine($"- **{displayName}**");
                }
                breakHelper.AppendLine();
            }
        }
    }

    private void GenerateNewComponentsSection(StringBuilder report)
    {
        if (_dataCoreComparisonRoot == null) return;

        var allFiles = _dataCoreComparisonRoot.GetAllFiles().ToArray();
        
        // Find added files in component directories
        var componentDirectories = new[]
        {
            "libs/foundry/records/entities/scitem/ships/armor",
            "libs/foundry/records/entities/scitem/ships/lifesupport", 
            "libs/foundry/records/entities/scitem/ships/shields",
            "libs/foundry/records/entities/scitem/ships/coolers",
            "libs/foundry/records/entities/scitem/ships/powerplants",
            "libs/foundry/records/entities/scitem/ships/quantumdrive",
            "libs/foundry/records/entities/scitem/ships/jumpmodule",
            "libs/foundry/records/entities/scitem/ships/thrusters"
        };
        
        var newComponents = allFiles
            .Where(f => f.Status == DataCoreComparisonStatus.Added)
            .Where(f => componentDirectories.Any(dir => f.FullPath.StartsWith(dir, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        if (newComponents.Length > 0)
        {
            report.AppendLine("## New Components Added");
            report.AppendLine();
            
            var componentGroups = new[]
            {
                ("Armor", "armor"),
                ("Life Support", "lifesupport"),
                ("Shields", "shields"), 
                ("Coolers", "coolers"),
                ("Power Plants", "powerplants"),
                ("Quantum Drives", "quantumdrive"),
                ("Jump Modules", "jumpmodule"),
                ("Thrusters", "thrusters")
            };
            
            foreach (var (groupName, pathSegment) in componentGroups)
            {
                var componentsInGroup = newComponents.Where(c => c.FullPath.Contains($"/{pathSegment}/", StringComparison.OrdinalIgnoreCase)).ToArray();
                if (componentsInGroup.Any())
                {
                    report.AppendLine($"### {groupName}");
                    foreach (var component in componentsInGroup.OrderBy(c => c.FullPath))
                    {
                        var componentName = Path.GetFileNameWithoutExtension(component.FullPath);
                        var displayName = CleanDisplayName(componentName);
                        var shortPath = GetShortPath(component.FullPath);
                        
                        report.AppendLine($"- **{displayName}** (`{shortPath}`)");
                    }
                    report.AppendLine();
                }
            }
        }
    }

    private void GenerateNewPaintsSection(StringBuilder report)
    {
        if (_dataCoreComparisonRoot == null) return;

        var allFiles = _dataCoreComparisonRoot.GetAllFiles().ToArray();
        
                 // Find added files in the paints directory
         var paintDirectories = new[]
         {
             "libs/foundry/records/entities/scitem/ships/paints"
         };
        
        var newPaints = allFiles
            .Where(f => f.Status == DataCoreComparisonStatus.Added)
            .Where(f => paintDirectories.Any(dir => f.FullPath.StartsWith(dir, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        if (newPaints.Length > 0)
        {
            report.AppendLine("## New Paints Added");
            report.AppendLine();
            
            foreach (var paint in newPaints.OrderBy(p => p.FullPath))
            {
                var paintName = Path.GetFileNameWithoutExtension(paint.FullPath);
                // Clean up the paint name for better readability
                var displayName = paintName.Replace("_", " ").Replace(".xml", "");
                
                report.AppendLine($"- **{displayName}**");
                report.AppendLine($"  - File: `{paint.FullPath}`");
            }
            report.AppendLine();
        }
    }

    private string CleanDisplayName(string name)
    {
        return name.Replace("_", " ").Replace(".xml", "");
    }
    
    private string GetShortPath(string fullPath)
    {
        // Extract just the meaningful part of the path
        if (fullPath.StartsWith("libs/foundry/records/entities/", StringComparison.OrdinalIgnoreCase))
        {
            return fullPath.Substring("libs/foundry/records/entities/".Length);
        }
        if (fullPath.StartsWith("libs/foundry/records/", StringComparison.OrdinalIgnoreCase))
        {
            return fullPath.Substring("libs/foundry/records/".Length);
        }
        return fullPath;
    }

    private class ItemInfo
    {
        public string DisplayName { get; set; } = "";
        public string Description { get; set; } = "";
        public string Manufacturer { get; set; } = "";
        public int Size { get; set; } = 0;
    }

    private ItemInfo? ExtractShipInfo(DataCoreComparisonFileNode fileNode, DataCoreDatabase database)
    {
        try
        {
            if (fileNode.RightRecord == null) return null;

            var record = fileNode.RightRecord.Value;
            var xmlContent = GenerateDataCoreRecordPreview(record, database);
            
            // Parse the XML content to extract ship info
            var info = new ItemInfo();
            
            // For ships, try to find localization keys based on filename patterns
            var fileName = Path.GetFileNameWithoutExtension(fileNode.Name);
            
            _logger.LogDebug("Processing ship file: {FileName}", fileName);
            
            // Try common vehicle name patterns
            var namePatterns = new[]
            {
                $"vehicle_Name{fileName}",
                $"vehicle_Name{fileName.ToUpperInvariant()}",
                $"vehicle_Name{fileName.Replace("_", "")}",
                // Try extracting manufacturer and ship name from filename (e.g., aegs_idris_m)
                GenerateVehicleNameKey(fileName)
            };
            
            _logger.LogDebug("Generated name patterns for {FileName}: {Patterns}", fileName, string.Join(", ", namePatterns.Where(p => !string.IsNullOrEmpty(p))));
            
            foreach (var pattern in namePatterns)
            {
                if (!string.IsNullOrEmpty(pattern) && _currentLocalizationData?.ContainsKey(pattern) == true)
                {
                    info.DisplayName = _currentLocalizationData[pattern];
                    _logger.LogDebug("Found ship name for {FileName}: {Pattern} -> {DisplayName}", fileName, pattern, info.DisplayName);
                    break;
                }
                else if (!string.IsNullOrEmpty(pattern))
                {
                    _logger.LogDebug("Ship name pattern not found for {FileName}: {Pattern}", fileName, pattern);
                }
            }
            
            // If no name found from localization, try extracting from XML or use filename
            if (string.IsNullOrEmpty(info.DisplayName))
            {
                info.DisplayName = ExtractDisplayName(xmlContent, fileNode.Name);
                
                // If we still got a placeholder or unhelpful name, create a nice name from filename
                if (string.IsNullOrEmpty(info.DisplayName) || 
                    info.DisplayName == "<= PLACEHOLDER =>" || 
                    info.DisplayName == "0" || 
                    info.DisplayName == "NULL" ||
                    info.DisplayName.StartsWith("@"))
                {
                    info.DisplayName = CreateShipNameFromFilename(fileName);
                }
            }
            
            // Try to find description using similar patterns
            var descPatterns = new[]
            {
                $"vehicle_Desc{fileName}",
                $"vehicle_Desc{fileName.ToUpperInvariant()}",
                $"vehicle_Desc{fileName.Replace("_", "")}",
                GenerateVehicleDescKey(fileName)
            };
            
            foreach (var pattern in descPatterns)
            {
                if (!string.IsNullOrEmpty(pattern) && _currentLocalizationData?.ContainsKey(pattern) == true)
                {
                    info.Description = _currentLocalizationData[pattern];
                    break;
                }
            }
            
            // If no description found from localization, try extracting from XML
            if (string.IsNullOrEmpty(info.Description))
            {
                info.Description = ExtractDescription(xmlContent);
            }
            
            info.Manufacturer = ExtractManufacturer(xmlContent);
            
            return info;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to extract ship info for {FileName}", fileNode.Name);
            return null;
        }
    }

    private ItemInfo? ExtractWeaponInfo(DataCoreComparisonFileNode fileNode, DataCoreDatabase database)
    {
        try
        {
            if (fileNode.RightRecord == null) return null;

            var record = fileNode.RightRecord.Value;
            var xmlContent = GenerateDataCoreRecordPreview(record, database);
            
            // Parse the XML content to extract weapon info
            var info = new ItemInfo();
            
            // For weapons, try to find localization keys based on filename patterns
            var fileName = Path.GetFileNameWithoutExtension(fileNode.Name);
            
            // Try common item name patterns for weapons/turrets/etc
            var namePatterns = new[]
            {
                $"item_Name{fileName}",
                $"item_Name{fileName.ToUpperInvariant()}",
                $"item_Name{fileName.Replace("_", "")}",
                GenerateItemNameKey(fileName),
                // Also try without the item_ prefix in case it's stored differently
                fileName,
                fileName.ToUpperInvariant(),
                fileName.Replace("_", "")
            };
            
            foreach (var pattern in namePatterns)
            {
                if (!string.IsNullOrEmpty(pattern) && _currentLocalizationData?.ContainsKey(pattern) == true)
                {
                    info.DisplayName = _currentLocalizationData[pattern];
                    break;
                }
            }
            
            // If no name found from localization, try extracting from XML or use filename
            if (string.IsNullOrEmpty(info.DisplayName))
            {
                info.DisplayName = ExtractDisplayName(xmlContent, fileNode.Name);
            }
            
            // Try to find description using similar patterns
            var descPatterns = new[]
            {
                $"item_Desc{fileName}",
                $"item_Desc{fileName.ToUpperInvariant()}",
                $"item_Desc{fileName.Replace("_", "")}",
                GenerateItemDescKey(fileName)
            };
            
            foreach (var pattern in descPatterns)
            {
                if (!string.IsNullOrEmpty(pattern) && _currentLocalizationData?.ContainsKey(pattern) == true)
                {
                    info.Description = _currentLocalizationData[pattern];
                    break;
                }
            }
            
            // If no description found from localization, try extracting from XML
            if (string.IsNullOrEmpty(info.Description))
            {
                info.Description = ExtractDescription(xmlContent);
            }
            
            info.Manufacturer = ExtractManufacturer(xmlContent);
            info.Size = ExtractSize(xmlContent);
            
            return info;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to extract weapon info for {FileName}", fileNode.Name);
            return null;
        }
    }

    private string ExtractDisplayName(string xmlContent, string fallbackName)
    {
        // Try to extract display name from XML
        var patterns = new[]
        {
            @"<Name>\s*([^<]+)\s*</Name>",
            @"<DisplayName>\s*([^<]+)\s*</DisplayName>",
            @"<ClassName>\s*([^<]+)\s*</ClassName>"
        };

        foreach (var pattern in patterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(xmlContent, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var name = match.Groups[1].Value.Trim();
                if (!string.IsNullOrEmpty(name) && name != "0" && name != "NULL")
                {
                    return ResolveLocalizationKey(name);
                }
            }
        }

        // Fallback to cleaned filename
        return CleanDisplayName(Path.GetFileNameWithoutExtension(fallbackName));
    }

    private string ExtractDescription(string xmlContent)
    {
        var patterns = new[]
        {
            @"<Description>\s*([^<]+)\s*</Description>",
            @"<desc>\s*([^<]+)\s*</desc>",
            @"<Desc>\s*([^<]+)\s*</Desc>"
        };

        foreach (var pattern in patterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(xmlContent, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var desc = match.Groups[1].Value.Trim();
                if (!string.IsNullOrEmpty(desc) && desc != "0" && desc != "NULL")
                {
                    return ResolveLocalizationKey(desc);
                }
            }
        }

        return "";
    }

    private string ExtractManufacturer(string xmlContent)
    {
        var patterns = new[]
        {
            @"<Manufacturer>\s*([^<]+)\s*</Manufacturer>",
            @"<manufacturer>\s*([^<]+)\s*</manufacturer>"
        };

        foreach (var pattern in patterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(xmlContent, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var manufacturer = match.Groups[1].Value.Trim();
                if (!string.IsNullOrEmpty(manufacturer) && manufacturer != "0" && manufacturer != "NULL")
                {
                    return ResolveLocalizationKey(manufacturer);
                }
            }
        }

        return "";
    }

    private int ExtractSize(string xmlContent)
    {
        var patterns = new[]
        {
            @"<Size>\s*(\d+)\s*</Size>",
            @"<size>\s*(\d+)\s*</size>",
            @"<WeaponSize>\s*(\d+)\s*</WeaponSize>",
            @"<HardpointSize>\s*(\d+)\s*</HardpointSize>"
        };

        foreach (var pattern in patterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(xmlContent, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var size))
            {
                return size;
            }
        }

        return 0;
    }

    private string ResolveLocalizationKey(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        
        // If it's a localization key (starts with @), try to resolve it
        if (text.StartsWith("@") && _currentLocalizationData != null)
        {
            var key = text.Substring(1); // Remove the @ prefix
            if (_currentLocalizationData.TryGetValue(key, out var localizedText))
            {
                return CleanDisplayName(localizedText);
            }
            
            // If not found, return the original text but cleaned
            return CleanDisplayName(text);
        }
        
        // If it's not a localization key, return as-is but cleaned
        return CleanDisplayName(text);
    }

    private string? GenerateVehicleNameKey(string fileName)
    {
        // Convert filename like "aegs_idris_m", "aegs_idris_p_fw_25", or "aegs_idris_k_test" to "vehicle_NameAEGS_Idris_M"
        if (string.IsNullOrEmpty(fileName)) return null;
        
        var parts = fileName.Split('_');
        if (parts.Length >= 3) // Expect at least manufacturer_ship_variant
        {
            // Extract manufacturer (AEGS), ship (Idris), and variant (M/P/K)
            var manufacturer = parts[0].ToUpperInvariant(); // AEGS
            var shipName = char.ToUpperInvariant(parts[1][0]) + parts[1].Substring(1).ToLowerInvariant(); // Idris
            
            // For variant, look through all remaining parts to find the main variant letter
            var variant = "";
            for (int i = 2; i < parts.Length; i++)
            {
                var part = parts[i].ToLowerInvariant();
                // Look for single letter variants (m, p, k) 
                if (part.Length == 1 && (part == "m" || part == "p" || part == "k"))
                {
                    variant = part.ToUpperInvariant();
                    break;
                }
                // Also check if part starts with variant letter
                else if (part.StartsWith("m") || part.StartsWith("p") || part.StartsWith("k"))
                {
                    var firstChar = part[0];
                    if (firstChar == 'm' || firstChar == 'p' || firstChar == 'k')
                    {
                        variant = firstChar.ToString().ToUpperInvariant();
                        break;
                    }
                }
            }
            
            if (!string.IsNullOrEmpty(variant))
            {
                return $"vehicle_Name{manufacturer}_{shipName}_{variant}";
            }
            else
            {
                return $"vehicle_Name{manufacturer}_{shipName}";
            }
        }
        
        return null;
    }

    private string? GenerateVehicleDescKey(string fileName)
    {
        // Convert filename like "aegs_idris_m", "aegs_idris_p_fw_25", or "aegs_idris_k_test" to "vehicle_DescAEGS_Idris_M"
        if (string.IsNullOrEmpty(fileName)) return null;
        
        var parts = fileName.Split('_');
        if (parts.Length >= 3) // Expect at least manufacturer_ship_variant
        {
            // Extract manufacturer (AEGS), ship (Idris), and variant (M/P/K)
            var manufacturer = parts[0].ToUpperInvariant(); // AEGS
            var shipName = char.ToUpperInvariant(parts[1][0]) + parts[1].Substring(1).ToLowerInvariant(); // Idris
            
            // For variant, look through all remaining parts to find the main variant letter
            var variant = "";
            for (int i = 2; i < parts.Length; i++)
            {
                var part = parts[i].ToLowerInvariant();
                // Look for single letter variants (m, p, k) 
                if (part.Length == 1 && (part == "m" || part == "p" || part == "k"))
                {
                    variant = part.ToUpperInvariant();
                    break;
                }
                // Also check if part starts with variant letter
                else if (part.StartsWith("m") || part.StartsWith("p") || part.StartsWith("k"))
                {
                    var firstChar = part[0];
                    if (firstChar == 'm' || firstChar == 'p' || firstChar == 'k')
                    {
                        variant = firstChar.ToString().ToUpperInvariant();
                        break;
                    }
                }
            }
            
            if (!string.IsNullOrEmpty(variant))
            {
                return $"vehicle_Desc{manufacturer}_{shipName}_{variant}";
            }
            else
            {
                return $"vehicle_Desc{manufacturer}_{shipName}";
            }
        }
        
        return null;
    }

    private string? GenerateItemNameKey(string fileName)
    {
        // Convert filename like "hrst_laserbeam_s10_bespoke" to "item_NameHRST_LaserBeam_S10_Bespoke"
        if (string.IsNullOrEmpty(fileName)) return null;
        
        var parts = fileName.Split('_');
        if (parts.Length >= 2)
        {
            // Capitalize each part, with special handling for size indicators
            var capitalizedParts = parts.Select(p => 
            {
                if (p.StartsWith("s") && p.Length > 1 && char.IsDigit(p[1]))
                {
                    return "S" + p.Substring(1); // Convert s10 to S10
                }
                return char.ToUpperInvariant(p[0]) + p.Substring(1).ToLowerInvariant();
            }).ToArray();
            return $"item_Name{string.Join("_", capitalizedParts)}";
        }
        
        return null;
    }

    private string? GenerateItemDescKey(string fileName)
    {
        // Convert filename like "hrst_laserbeam_s10_bespoke" to "item_DescHRST_LaserBeam_S10_Bespoke"
        if (string.IsNullOrEmpty(fileName)) return null;
        
        var parts = fileName.Split('_');
        if (parts.Length >= 2)
        {
            // Capitalize each part, with special handling for size indicators
            var capitalizedParts = parts.Select(p => 
            {
                if (p.StartsWith("s") && p.Length > 1 && char.IsDigit(p[1]))
                {
                    return "S" + p.Substring(1); // Convert s10 to S10
                }
                return char.ToUpperInvariant(p[0]) + p.Substring(1).ToLowerInvariant();
            }).ToArray();
            return $"item_Desc{string.Join("_", capitalizedParts)}";
        }
        
        return null;
    }

    private string CreateShipNameFromFilename(string fileName)
    {
        // Convert filenames like "aegs_idris_k_test" to "Aegis Idris-K"
        if (string.IsNullOrEmpty(fileName)) return "Unknown Ship";
        
        var parts = fileName.Split('_');
        if (parts.Length >= 3)
        {
            // Extract manufacturer, ship name, and variant
            var manufacturer = parts[0].ToLowerInvariant() switch
            {
                "aegs" => "Aegis",
                "anvl" => "Anvil",
                "argo" => "Argo",
                "banu" => "Banu",
                "cnou" => "Consolidated Outland",
                "crus" => "Crusader",
                "drke" => "Drake",
                "espr" => "Esperia",
                "grin" => "Greycat",
                "krig" => "Kruger",
                "misc" => "MISC",
                "orig" => "Origin",
                "rsi" => "RSI",
                "vncl" => "Vanduul",
                _ => char.ToUpperInvariant(parts[0][0]) + parts[0].Substring(1).ToLowerInvariant()
            };
            
            var shipName = char.ToUpperInvariant(parts[1][0]) + parts[1].Substring(1).ToLowerInvariant();
            
            // Find variant (M, P, K, etc.)
            var variant = "";
            for (int i = 2; i < parts.Length; i++)
            {
                var part = parts[i].ToLowerInvariant();
                if (part.Length == 1 && (part == "m" || part == "p" || part == "k" || part == "c"))
                {
                    variant = "-" + part.ToUpperInvariant();
                    break;
                }
                else if (part.StartsWith("m") || part.StartsWith("p") || part.StartsWith("k") || part.StartsWith("c"))
                {
                    var firstChar = part[0];
                    if (firstChar == 'm' || firstChar == 'p' || firstChar == 'k' || firstChar == 'c')
                    {
                        variant = "-" + firstChar.ToString().ToUpperInvariant();
                        break;
                    }
                }
            }
            
            return $"{manufacturer} {shipName}{variant}";
        }
        else if (parts.Length == 2)
        {
            // Simple manufacturer_ship format
            var manufacturer = char.ToUpperInvariant(parts[0][0]) + parts[0].Substring(1).ToLowerInvariant();
            var shipName = char.ToUpperInvariant(parts[1][0]) + parts[1].Substring(1).ToLowerInvariant();
            return $"{manufacturer} {shipName}";
        }
        
        // Fallback to cleaned filename
        return CleanDisplayName(fileName);
    }

    private void GenerateChangedFilesSection(StringBuilder report)
    {
        if (_dataCoreComparisonRoot == null) return;

        var allFiles = _dataCoreComparisonRoot.GetAllFiles().ToArray();
        
        // Added Files
        var addedFiles = allFiles.Where(f => f.Status == DataCoreComparisonStatus.Added).ToArray();
        if (addedFiles.Length > 0)
        {
            var breakHelper = new DiscordBreakHelper(report);
            breakHelper.AppendLine("## Added Files");
            var consolidatedFiles = ConsolidateRelatedFiles(addedFiles.Select(f => f.FullPath));
            foreach (var file in consolidatedFiles.OrderBy(f => f))
            {
                breakHelper.AppendLine($"- `{file}`");
            }
            breakHelper.AppendLine();
        }

        // Removed Files
        var removedFiles = allFiles.Where(f => f.Status == DataCoreComparisonStatus.Removed).ToArray();
        if (removedFiles.Length > 0)
        {
            var breakHelper = new DiscordBreakHelper(report);
            breakHelper.AppendLine("## Removed Files");
            var consolidatedFiles = ConsolidateRelatedFiles(removedFiles.Select(f => f.FullPath));
            foreach (var file in consolidatedFiles.OrderBy(f => f))
            {
                breakHelper.AppendLine($"- `{file}`");
            }
            breakHelper.AppendLine();
        }

        // Modified Files
        var modifiedFiles = allFiles.Where(f => f.Status == DataCoreComparisonStatus.Modified).ToArray();
        if (modifiedFiles.Length > 0)
        {
            var breakHelper = new DiscordBreakHelper(report);
            breakHelper.AppendLine("## Modified Files");
            var consolidatedFiles = ConsolidateRelatedFiles(modifiedFiles.Select(f => f.FullPath));
            foreach (var file in consolidatedFiles.OrderBy(f => f))
            {
                breakHelper.AppendLine($"- `{file}`");
            }
            breakHelper.AppendLine();
        }
    }

    private async Task GenerateLocalizationChangesSection(StringBuilder report)
    {
        if (_leftDataCoreDatabase == null || _rightDataCoreDatabase == null) return;

        try
        {
            // Extract localization data from both P4K files
            var leftLocalization = await ExtractLocalizationData(LeftDataCoreP4kPath);
            var rightLocalization = await ExtractLocalizationData(RightDataCoreP4kPath);

            if (leftLocalization != null && rightLocalization != null)
            {
                var addedStrings = rightLocalization.Where(kvp => !leftLocalization.ContainsKey(kvp.Key)).ToArray();
                var changedStrings = rightLocalization.Where(kvp => leftLocalization.ContainsKey(kvp.Key) && leftLocalization[kvp.Key] != kvp.Value).ToArray();
                var removedStrings = leftLocalization.Where(kvp => !rightLocalization.ContainsKey(kvp.Key)).ToArray();

                // Added Localization
                if (addedStrings.Length > 0)
                {
                    report.AppendLine("## Added Localization");
                    report.AppendLine("```");
                    foreach (var kvp in addedStrings.OrderBy(x => x.Key))
                    {
                        report.AppendLine($"{kvp.Key}={kvp.Value}");
                    }
                    report.AppendLine("```");
                    report.AppendLine();
                }

                // Modified Localization Strings
                if (changedStrings.Length > 0)
                {
                    report.AppendLine("## Modified Localization Strings");
                    foreach (var kvp in changedStrings.OrderBy(x => x.Key))
                    {
                        report.AppendLine($"- **{kvp.Key}:**");
                        report.AppendLine($"  - **Old:** `{leftLocalization[kvp.Key]}`");
                        report.AppendLine($"  - **New:** `{kvp.Value}`");
                    }
                    report.AppendLine();
                }

                // Removed Localization Strings
                if (removedStrings.Length > 0)
                {
                    report.AppendLine("## Removed Localization Strings");
                    foreach (var kvp in removedStrings.OrderBy(x => x.Key))
                    {
                        report.AppendLine($"- **{kvp.Key}:** `{kvp.Value}`");
                    }
                    report.AppendLine();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract localization data for report");
            report.AppendLine("## Localization Changes");
            report.AppendLine("*Unable to extract localization data*");
            report.AppendLine();
        }
    }

    private async Task<Dictionary<string, string>?> ExtractLocalizationData(string p4kPath)
    {
        try
        {
            var p4kFileSystem = new P4kFileSystem(P4kFile.FromFile(p4kPath));
            
            // Look for localization files
            string[] localizationPaths = {
                "Data/Localization/english/global.ini",
                "Data\\Localization\\english\\global.ini"
            };

            foreach (var path in localizationPaths)
            {
                if (p4kFileSystem.FileExists(path))
                {
                    using var stream = p4kFileSystem.OpenRead(path);
                    using var reader = new StreamReader(stream, Encoding.UTF8);
                    var content = await reader.ReadToEndAsync();
                    
                    var localizationData = new Dictionary<string, string>();
                    var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    
                    foreach (var line in lines)
                    {
                        var trimmedLine = line.Trim();
                        if (trimmedLine.StartsWith(";") || trimmedLine.StartsWith("#") || string.IsNullOrWhiteSpace(trimmedLine))
                            continue; // Skip comments and empty lines
                        
                        var equalIndex = trimmedLine.IndexOf('=');
                        if (equalIndex > 0)
                        {
                            var key = trimmedLine.Substring(0, equalIndex).Trim();
                            var value = trimmedLine.Substring(equalIndex + 1).Trim();
                            localizationData[key] = value;
                        }
                    }
                    
                    return localizationData;
                }
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract localization data from P4K: {P4kPath}", p4kPath);
            return null;
        }
    }

    [RelayCommand]
    public async Task CreateP4kReport()
    {
        if (!CanCreateP4kReport)
        {
            _logger.LogWarning("Cannot create P4K report - no comparison data available");
            return;
        }

        try
        {
            if (string.IsNullOrWhiteSpace(P4kOutputDirectory))
            {
                _logger.LogWarning("P4K output directory not configured");
                ComparisonStatus = "Please configure P4K output directory first";
            return;
        }

            Directory.CreateDirectory(P4kOutputDirectory);
            
            var leftVersion = SanitizeForFilename(ExtractVersionFromManifest(LeftP4kPath));
            var rightVersion = SanitizeForFilename(ExtractVersionFromManifest(RightP4kPath));
            var reportFileName = $"P4K_Comparison_{leftVersion}_to_{rightVersion}.md";
            var reportPath = Path.Combine(P4kOutputDirectory, reportFileName);

        IsComparing = true;
            ComparisonStatus = "Generating P4K report...";

            await Task.Run(async () =>
            {
                try
                {
                    var report = await GenerateP4kComparisonReport();
                    
                    await File.WriteAllTextAsync(reportPath, report, Encoding.UTF8);
                    
                    Dispatcher.UIThread.Post(() =>
                    {
                        ComparisonStatus = $"P4K report saved successfully to {reportFileName}";
                        _logger.LogInformation("P4K comparison report saved to: {FilePath}", reportPath);
                    });
        }
        catch (Exception ex)
        {
                    _logger.LogError(ex, "Failed to generate or save P4K report");
                    Dispatcher.UIThread.Post(() => ComparisonStatus = $"Failed to save P4K report: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating P4K report");
            ComparisonStatus = $"Error creating P4K report: {ex.Message}";
        }
        finally
        {
            IsComparing = false;
        }
    }

    private async Task<string> GenerateP4kComparisonReport()
    {
        if (_comparisonRoot == null || _leftP4kFile == null || _rightP4kFile == null)
            throw new InvalidOperationException("No P4K comparison data available");

        var report = new StringBuilder();
        var stats = P4kComparison.AnalyzeComparison(_comparisonRoot);

        // Header
        var leftVersion = ExtractVersionFromManifest(LeftP4kPath);
        var rightVersion = ExtractVersionFromManifest(RightP4kPath);
        
        report.AppendLine($"# P4K Comparison Report: {leftVersion} → {rightVersion}");
        report.AppendLine($"**Generated:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine($"**Left P4K:** {Path.GetFileName(LeftP4kPath)} (v{leftVersion})");
        report.AppendLine($"**Right P4K:** {Path.GetFileName(RightP4kPath)} (v{rightVersion})");
        report.AppendLine();

        // Summary
        report.AppendLine("## Summary");
        report.AppendLine($"- **Total Files:** {stats.TotalFiles}");
        report.AppendLine($"- **Added:** {stats.AddedFiles}");
        report.AppendLine($"- **Removed:** {stats.RemovedFiles}");
        report.AppendLine($"- **Modified:** {stats.ModifiedFiles}");
        report.AppendLine($"- **Unchanged:** {stats.UnchangedFiles}");
        report.AppendLine();
        
        // === DISCORD BREAK === (Copy sections below separately for Discord's 4000 char limit)
        report.AppendLine("---");
        report.AppendLine();

        // Changed Files Section
        GenerateP4kChangedFilesSection(report);
        
        report.AppendLine("---");
        report.AppendLine();
        
        // Localization Changes Section (extract from both P4K files)
        await GenerateP4kLocalizationChangesSection(report);

        return report.ToString();
    }

    private void GenerateP4kChangedFilesSection(StringBuilder report)
    {
        if (_comparisonRoot == null) return;

        var allFiles = _comparisonRoot.GetAllFiles().ToArray();
        
        // Added Files with better categorization
        var addedFiles = allFiles.Where(f => f.Status == P4kComparisonStatus.Added).ToArray();
        if (addedFiles.Length > 0)
        {
            GenerateP4kAssetSummary(report, "Added", addedFiles);
        }

        // Removed Files
        var removedFiles = allFiles.Where(f => f.Status == P4kComparisonStatus.Removed).ToArray();
        if (removedFiles.Length > 0)
        {
            GenerateP4kAssetSummary(report, "Removed", removedFiles);
        }

        // Modified Files
        var modifiedFiles = allFiles.Where(f => f.Status == P4kComparisonStatus.Modified).ToArray();
        if (modifiedFiles.Length > 0)
        {
            GenerateP4kAssetSummary(report, "Modified", modifiedFiles);
        }
    }

    private void GenerateP4kAssetSummary(StringBuilder report, string changeType, P4kComparisonFileNode[] files)
    {
        report.AppendLine($"## {changeType} Assets");
        report.AppendLine();
        
        // Filter out numbered backup files (.dds.1, .dds.2, etc.)
        var filteredFiles = files.Where(f => !IsNumberedBackupFile(f.FullPath)).ToArray();
        
        // Categorize files by type - order matters for exclusions
        var audioFiles = filteredFiles.Where(f => f.FullPath.EndsWith(".wem", StringComparison.OrdinalIgnoreCase)).ToArray();
        
        var animations = filteredFiles.Where(f => 
            f.FullPath.Contains("Animations\\", StringComparison.OrdinalIgnoreCase) || 
            f.FullPath.Contains("Animations/", StringComparison.OrdinalIgnoreCase) ||
            f.FullPath.EndsWith(".caf", StringComparison.OrdinalIgnoreCase) ||
            f.FullPath.EndsWith(".dba", StringComparison.OrdinalIgnoreCase) ||
            f.FullPath.EndsWith(".adb", StringComparison.OrdinalIgnoreCase) ||
            f.FullPath.EndsWith(".i_caf", StringComparison.OrdinalIgnoreCase) ||
            f.FullPath.EndsWith(".chrparams", StringComparison.OrdinalIgnoreCase)).ToArray();
            
        // UI files should be categorized before textures to catch UI .dds files
        var uiFiles = filteredFiles.Where(f => !animations.Contains(f) && (
            f.FullPath.StartsWith("UI\\", StringComparison.OrdinalIgnoreCase) ||
            f.FullPath.StartsWith("UI/", StringComparison.OrdinalIgnoreCase) ||
            f.FullPath.Contains("\\UI\\", StringComparison.OrdinalIgnoreCase) ||
            f.FullPath.Contains("/UI/", StringComparison.OrdinalIgnoreCase) ||
            f.FullPath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))).ToArray();
            
        var models = filteredFiles.Where(f => !animations.Contains(f) && !uiFiles.Contains(f) && (
            f.FullPath.EndsWith(".cga", StringComparison.OrdinalIgnoreCase) ||
            f.FullPath.EndsWith(".cgam", StringComparison.OrdinalIgnoreCase) ||
            f.FullPath.EndsWith(".cgf", StringComparison.OrdinalIgnoreCase) ||
            f.FullPath.EndsWith(".cgfm", StringComparison.OrdinalIgnoreCase) ||
            f.FullPath.EndsWith(".meshsetup", StringComparison.OrdinalIgnoreCase))).ToArray();
            
        var textures = filteredFiles.Where(f => !animations.Contains(f) && !uiFiles.Contains(f) && !models.Contains(f) && (
            f.FullPath.EndsWith(".dds", StringComparison.OrdinalIgnoreCase) ||
            f.FullPath.EndsWith(".mtl", StringComparison.OrdinalIgnoreCase))).ToArray();
            
        var gameAudio = filteredFiles.Where(f => !animations.Contains(f) && !uiFiles.Contains(f) && !models.Contains(f) && !textures.Contains(f) && (
            f.FullPath.Contains("GameAudio", StringComparison.OrdinalIgnoreCase) ||
            f.FullPath.EndsWith(".bnk", StringComparison.OrdinalIgnoreCase) ||
            (f.FullPath.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) && 
             (f.FullPath.Contains("GameAudio", StringComparison.OrdinalIgnoreCase) || 
              f.FullPath.Contains("Particles", StringComparison.OrdinalIgnoreCase))))).ToArray();
              
        var particles = filteredFiles.Where(f => !animations.Contains(f) && !uiFiles.Contains(f) && !models.Contains(f) && !textures.Contains(f) && !gameAudio.Contains(f) &&
            f.FullPath.Contains("Particles", StringComparison.OrdinalIgnoreCase)).ToArray();
            
        var misc = filteredFiles.Except(audioFiles).Except(animations).Except(uiFiles).Except(models).Except(textures)
                       .Except(gameAudio).Except(particles).ToArray();

        var breakHelper = new DiscordBreakHelper(report);

        // Audio Files Summary
        if (audioFiles.Length > 0)
        {
            breakHelper.AppendLine($"### Audio Files ({audioFiles.Length} files)");
            breakHelper.AppendLine($"- {audioFiles.Length} WEM audio files added (compressed game audio)");
            breakHelper.AppendLine();
        }

        // Game Audio Configuration
        if (gameAudio.Length > 0)
        {
            breakHelper.AppendLine($"### Game Audio Configuration");
            var consolidatedAudio = ConsolidateRelatedFiles(gameAudio.Select(f => f.FullPath));
            foreach (var file in consolidatedAudio.OrderBy(f => f))
            {
                breakHelper.AppendLine($"- `{GetP4kShortPath(file)}`");
            }
            breakHelper.AppendLine();
        }

        // Animations
        if (animations.Length > 0)
        {
            breakHelper.AppendLine($"### Animations");
            var consolidatedAnimations = ConsolidateRelatedFiles(animations.Select(f => f.FullPath));
            foreach (var file in consolidatedAnimations.OrderBy(f => f))
            {
                breakHelper.AppendLine($"- `{GetP4kShortPath(file)}`");
            }
            breakHelper.AppendLine();
        }

        // 3D Models & Meshes
        if (models.Length > 0)
        {
            breakHelper.AppendLine($"### 3D Models & Meshes");
            var consolidatedModels = ConsolidateRelatedFiles(models.Select(f => f.FullPath));
            foreach (var file in consolidatedModels.OrderBy(f => f))
            {
                breakHelper.AppendLine($"- `{GetP4kShortPath(file)}`");
            }
            breakHelper.AppendLine();
        }

        // Textures & Materials
        if (textures.Length > 0)
        {
            breakHelper.AppendLine($"### Textures & Materials");
            var consolidatedTextures = ConsolidateRelatedFiles(textures.Select(f => f.FullPath));
            foreach (var file in consolidatedTextures.OrderBy(f => f))
            {
                breakHelper.AppendLine($"- `{GetP4kShortPath(file)}`");
            }
            breakHelper.AppendLine();
        }

        // UI Assets
        if (uiFiles.Length > 0)
        {
            breakHelper.AppendLine($"### UI Assets");
            var consolidatedUI = ConsolidateRelatedFiles(uiFiles.Select(f => f.FullPath));
            foreach (var file in consolidatedUI.OrderBy(f => f))
            {
                breakHelper.AppendLine($"- `{GetP4kShortPath(file)}`");
            }
            breakHelper.AppendLine();
        }

        // Particle Effects
        if (particles.Length > 0)
        {
            breakHelper.AppendLine($"### Particle Effects");
            var consolidatedParticles = ConsolidateRelatedFiles(particles.Select(f => f.FullPath));
            foreach (var file in consolidatedParticles.OrderBy(f => f))
            {
                breakHelper.AppendLine($"- `{GetP4kShortPath(file)}`");
            }
            breakHelper.AppendLine();
        }

        // Other Files
        if (misc.Length > 0)
        {
            breakHelper.AppendLine($"### Other Files");
            var consolidatedMisc = ConsolidateRelatedFiles(misc.Select(f => f.FullPath));
            foreach (var file in consolidatedMisc.OrderBy(f => f))
            {
                breakHelper.AppendLine($"- `{GetP4kShortPath(file)}`");
            }
            breakHelper.AppendLine();
        }
    }

    private string GetP4kShortPath(string fullPath)
    {
        // Remove Data\ prefix for P4K files to make paths shorter
        if (fullPath.StartsWith("Data\\", StringComparison.OrdinalIgnoreCase))
        {
            return fullPath.Substring(5);
        }
        if (fullPath.StartsWith("Data/", StringComparison.OrdinalIgnoreCase))
        {
            return fullPath.Substring(5);
        }
        return fullPath;
    }

    private static bool IsNumberedBackupFile(string filePath)
    {
        // Check if file ends with a pattern like .dds.1, .dds.2, .mtl.3, etc.
        var fileName = Path.GetFileName(filePath);
        var parts = fileName.Split('.');
        
        // Need at least 3 parts: name.extension.number
        if (parts.Length < 3) return false;
        
        // Last part should be a number
        if (!int.TryParse(parts[^1], out _)) return false;
        
        // Second to last part should be a known file extension
        var extension = parts[^2].ToLowerInvariant();
        var knownExtensions = new[] { "dds", "mtl", "cga", "cgam", "cgf", "cgfm", "meshsetup", "xml", "txt" };
        
        return knownExtensions.Contains(extension);
    }

    private class DiscordBreakHelper
    {
        private readonly StringBuilder _report;
        private const int MaxLength = 3800; // Leave some buffer before 4000
        private int _currentSectionLength;

        public DiscordBreakHelper(StringBuilder report)
        {
            _report = report;
            _currentSectionLength = 0;
        }

        public void AppendLine(string line = "")
        {
            var lineLength = line.Length + Environment.NewLine.Length;
            
            if (_currentSectionLength + lineLength > MaxLength && _currentSectionLength > 0)
            {
                _report.AppendLine();
                _report.AppendLine("---");
                _report.AppendLine();
                _currentSectionLength = 0;
            }
            
            _report.AppendLine(line);
            _currentSectionLength += lineLength;
        }

        public void ResetSection()
        {
            _currentSectionLength = 0;
        }

        public void ForceBreak()
        {
            if (_currentSectionLength > 0)
            {
                _report.AppendLine();
                _report.AppendLine("---");
                _report.AppendLine();
                _currentSectionLength = 0;
            }
        }
    }

    private async Task GenerateP4kLocalizationChangesSection(StringBuilder report)
    {
        if (_leftP4kFile == null || _rightP4kFile == null) return;

        try
        {
            // Extract localization data from both P4K files
            var leftLocalization = await ExtractP4kLocalizationData(_leftP4kFile);
            var rightLocalization = await ExtractP4kLocalizationData(_rightP4kFile);

            if (leftLocalization != null && rightLocalization != null)
            {
                var addedStrings = rightLocalization.Where(kvp => !leftLocalization.ContainsKey(kvp.Key)).ToArray();
                var changedStrings = rightLocalization.Where(kvp => leftLocalization.ContainsKey(kvp.Key) && leftLocalization[kvp.Key] != kvp.Value).ToArray();
                var removedStrings = leftLocalization.Where(kvp => !rightLocalization.ContainsKey(kvp.Key)).ToArray();

                // Added Localization
                if (addedStrings.Length > 0)
                {
                    report.AppendLine("## Added Localization");
                    report.AppendLine("```");
                    foreach (var kvp in addedStrings.OrderBy(x => x.Key))
                    {
                        report.AppendLine($"{kvp.Key}={kvp.Value}");
                    }
                    report.AppendLine("```");
                    report.AppendLine();
                }

                // Modified Localization Strings
                if (changedStrings.Length > 0)
                {
                    report.AppendLine("## Modified Localization Strings");
                    foreach (var kvp in changedStrings.OrderBy(x => x.Key))
                    {
                        report.AppendLine($"- **{kvp.Key}:**");
                        report.AppendLine($"  - **Old:** `{leftLocalization[kvp.Key]}`");
                        report.AppendLine($"  - **New:** `{kvp.Value}`");
                    }
                    report.AppendLine();
                }

                // Removed Localization Strings
                if (removedStrings.Length > 0)
                {
                    report.AppendLine("## Removed Localization Strings");
                    foreach (var kvp in removedStrings.OrderBy(x => x.Key))
                    {
                        report.AppendLine($"- **{kvp.Key}:** `{kvp.Value}`");
                    }
                    report.AppendLine();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract localization data for P4K report");
            report.AppendLine("## Localization Changes");
            report.AppendLine("*Unable to extract localization data*");
            report.AppendLine();
        }
    }

    private async Task<Dictionary<string, string>?> ExtractP4kLocalizationData(P4kFile p4kFile)
    {
        try
        {
            var p4kFileSystem = new P4kFileSystem(p4kFile);
            
            // Look for localization files
            string[] localizationPaths = {
                "Data/Localization/english/global.ini",
                "Data\\Localization\\english\\global.ini"
            };

            foreach (var path in localizationPaths)
            {
                if (p4kFileSystem.FileExists(path))
                {
                    using var stream = p4kFileSystem.OpenRead(path);
                    using var reader = new StreamReader(stream, Encoding.UTF8);
                    var content = await reader.ReadToEndAsync();
                    
                    var localizationData = new Dictionary<string, string>();
                    var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    
                    foreach (var line in lines)
                    {
                        var trimmedLine = line.Trim();
                        if (trimmedLine.StartsWith(";") || trimmedLine.StartsWith("#") || string.IsNullOrWhiteSpace(trimmedLine))
                            continue; // Skip comments and empty lines
                        
                        var equalIndex = trimmedLine.IndexOf('=');
                        if (equalIndex > 0)
                        {
                            var key = trimmedLine.Substring(0, equalIndex).Trim();
                            var value = trimmedLine.Substring(equalIndex + 1).Trim();
                            localizationData[key] = value;
                        }
                    }
                    
                    return localizationData;
                }
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract localization data from P4K file");
            return null;
        }
    }

    [RelayCommand]
    public async Task ExtractNewDdsFiles()
    {
        if (!CanExtractNewDdsFiles)
        {
            _logger.LogWarning("Cannot extract new DDS files - no comparison data or added DDS files available");
            return;
        }

        try
        {
            if (string.IsNullOrWhiteSpace(P4kOutputDirectory))
            {
                _logger.LogWarning("P4K output directory not configured");
                ComparisonStatus = "Please configure P4K output directory first";
                return;
            }

            var outputFolder = Path.Combine(P4kOutputDirectory, "DDS_Files");
            Directory.CreateDirectory(outputFolder);

            // Clear existing files to ensure a clean extraction of only current comparison files
            foreach (var file in Directory.GetFiles(outputFolder))
            {
                var ext = Path.GetExtension(file).ToLowerInvariant();
                if (ext == ".jpg" || ext == ".jpeg" || ext == ".png")
                {
                    File.Delete(file);
                }
            }

            IsComparing = true;
            ComparisonStatus = "Extracting new and modified DDS files...";

            await Task.Run(() =>
            {
                try
                {
                    // Get all added and modified DDS files
                    var ddsFiles = _comparisonRoot!.GetAllFiles()
                        .Where(f => f.Status == P4kComparisonStatus.Added || f.Status == P4kComparisonStatus.Modified)
                        .Where(f => Path.GetFileName(f.FullPath).Contains(".dds", StringComparison.OrdinalIgnoreCase))
                        .Where(f => f.RightEntry != null)
                        .ToArray();

                    if (ddsFiles.Length == 0)
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            ComparisonStatus = "No new or modified DDS files found to extract.";
                            _logger.LogInformation("No new or modified DDS files found to extract");
                        });
                        return;
                    }

                    var p4kFileSystem = new P4kFileSystem(_rightP4kFile!);
                    var extractedCount = 0;
                    var failedCount = 0;

                    for (int i = 0; i < ddsFiles.Length; i++)
                    {
                        var file = ddsFiles[i];
                        
                        Dispatcher.UIThread.Post(() => 
                            ComparisonStatus = $"Extracting DDS files... {i + 1}/{ddsFiles.Length}");

                        try
                        {
                            // Extract just the filename without directory structure
                            var originalFileName = Path.GetFileName(file.FullPath);
                            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(originalFileName);
                            
                            // Remove .dds from the filename if it's there
                            if (fileNameWithoutExt.EndsWith(".dds", StringComparison.OrdinalIgnoreCase))
                            {
                                fileNameWithoutExt = fileNameWithoutExt.Substring(0, fileNameWithoutExt.Length - 4);
                            }
                            
                            // Add "_modified" suffix for modified files
                            if (file.Status == P4kComparisonStatus.Modified)
                            {
                                fileNameWithoutExt += "_modified";
                            }
                            
                            // Create output path (overwrite existing files)
                            var jpgOutputPath = Path.Combine(outputFolder, fileNameWithoutExt + ".jpg");

                            // Extract and convert DDS to JPEG
                            var ms = DdsFile.MergeToStream(file.RightEntry!.Name, p4kFileSystem);
                            using var jpgStream = DdsFile.ConvertToJpeg(ms.ToArray(), false);
                            
                            File.WriteAllBytes(jpgOutputPath, jpgStream.ToArray());
                            extractedCount++;
                            
                            _logger.LogDebug("Extracted DDS file: {SourcePath} -> {OutputPath}", file.FullPath, jpgOutputPath);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to extract DDS file: {FileName}", file.FullPath);
                            failedCount++;
                        }
                    }

                    Dispatcher.UIThread.Post(() =>
                    {
                        ComparisonStatus = $"DDS extraction complete! Extracted: {extractedCount}, Failed: {failedCount}";
                        _logger.LogInformation("DDS extraction completed - Extracted: {Extracted}, Failed: {Failed}, Output folder: {OutputFolder}", 
                            extractedCount, failedCount, outputFolder);
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to extract new and modified DDS files");
                    Dispatcher.UIThread.Post(() => ComparisonStatus = $"DDS extraction failed: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting new and modified DDS files");
            ComparisonStatus = $"Error extracting DDS files: {ex.Message}";
        }
        finally
        {
            IsComparing = false;
        }
    }

    [RelayCommand]
    public async Task ExtractNewAudioFiles()
    {
        if (!CanExtractNewAudioFiles)
        {
            _logger.LogWarning("Cannot extract new audio files - no comparison data or added audio files available");
            return;
        }

        try
        {
            if (string.IsNullOrWhiteSpace(P4kOutputDirectory))
            {
                _logger.LogWarning("P4K output directory not configured");
                ComparisonStatus = "Please configure P4K output directory first";
                return;
            }

            var outputFolder = Path.Combine(P4kOutputDirectory, "WEM_Audio_Files");
            Directory.CreateDirectory(outputFolder);

            // Clear existing files to ensure a clean extraction of only current comparison files
            foreach (var file in Directory.GetFiles(outputFolder))
            {
                var ext = Path.GetExtension(file).ToLowerInvariant();
                if (ext == ".wem" || ext == ".ogg" || ext == ".mp3")
                {
                    File.Delete(file);
                }
            }

            IsComparing = true;
            ComparisonStatus = "Extracting new WEM audio files...";

            await Task.Run(() =>
            {
                try
                {
                    // Get all added WEM files
                    var addedWemFiles = _comparisonRoot!.GetAllFiles()
                        .Where(f => f.Status == P4kComparisonStatus.Added)
                        .Where(f => IsAudioFile(f.FullPath))
                        .Where(f => f.RightEntry != null)
                        .ToArray();

                    if (addedWemFiles.Length == 0)
                    {
        Dispatcher.UIThread.Post(() =>
        {
                            ComparisonStatus = "No new WEM audio files found to extract.";
                            _logger.LogInformation("No new WEM audio files found to extract");
                        });
                        return;
                    }

                    var p4kFileSystem = new P4kFileSystem(_rightP4kFile!);
                    var extractedCount = 0;
                    var failedCount = 0;

                    for (int i = 0; i < addedWemFiles.Length; i++)
                    {
                        var file = addedWemFiles[i];
                        
                        Dispatcher.UIThread.Post(() => 
                            ComparisonStatus = $"Extracting WEM files... {i + 1}/{addedWemFiles.Length}");

                        try
                        {
                            // Extract just the filename without directory structure
                            var originalFileName = Path.GetFileName(file.FullPath);
                            
                            // Create output path (overwrite existing files)
                            var outputPath = Path.Combine(outputFolder, originalFileName);

                            // Extract WEM file (supports nested .socpak/.pak)
                            using var entryStream = OpenEntryStream(file, useLeft: false);
                            using var outputStream = File.Create(outputPath);
                            entryStream.CopyTo(outputStream);
                            
                            extractedCount++;
                            
                            _logger.LogDebug("Extracted WEM file: {SourcePath} -> {OutputPath}", file.FullPath, outputPath);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to extract WEM file: {FileName}", file.FullPath);
                            failedCount++;
                        }
                    }

                    Dispatcher.UIThread.Post(() =>
                    {
                        ComparisonStatus = $"WEM extraction complete! Extracted: {extractedCount}, Failed: {failedCount}";
                        _logger.LogInformation("WEM extraction completed - Extracted: {Extracted}, Failed: {Failed}, Output folder: {OutputFolder}", 
                            extractedCount, failedCount, outputFolder);
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to extract new WEM audio files");
                    Dispatcher.UIThread.Post(() => ComparisonStatus = $"WEM extraction failed: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting new WEM audio files");
            ComparisonStatus = $"Error extracting WEM files: {ex.Message}";
        }
        finally
        {
            IsComparing = false;
        }
    }

    [RelayCommand]
    public async Task ExtractSelectedP4kFiles()
    {
        if (!CanExtractSelectedP4kFiles)
        {
            _logger.LogWarning("Cannot extract selected P4K files - no files selected or output directory not configured");
            return;
        }

        try
        {
            var outputFolder = Path.Combine(P4kOutputDirectory, "Selected_P4K_Files");
            Directory.CreateDirectory(outputFolder);

            IsComparing = true;
            ComparisonStatus = "Extracting selected P4K files...";

            await Task.Run(() =>
            {
                try
                {
                    var selectedFiles = SelectedP4kFiles.OfType<P4kComparisonFileNode>().ToArray();
                    var extractedCount = 0;
                    var failedCount = 0;

                    for (int i = 0; i < selectedFiles.Length; i++)
                    {
                        var file = selectedFiles[i];
                        
                        Dispatcher.UIThread.Post(() => 
                            ComparisonStatus = $"Extracting selected files... {i + 1}/{selectedFiles.Length}");

                        try
                        {
                            // Determine which P4K to extract from based on file status
                            P4kFile? sourceP4k = file.Status switch
                            {
                                P4kComparisonStatus.Added => _rightP4kFile,
                                P4kComparisonStatus.Removed => _leftP4kFile,
                                P4kComparisonStatus.Modified => _rightP4kFile,
                                P4kComparisonStatus.Unchanged => _leftP4kFile,
                                _ => _leftP4kFile
                            };

                            var zipEntry = file.Status switch
                            {
                                P4kComparisonStatus.Added => file.RightEntry,
                                P4kComparisonStatus.Removed => file.LeftEntry,
                                P4kComparisonStatus.Modified => file.RightEntry,
                                P4kComparisonStatus.Unchanged => file.LeftEntry ?? file.RightEntry,
                                _ => file.LeftEntry ?? file.RightEntry
                            };

                            if (sourceP4k == null || zipEntry == null)
                            {
                                _logger.LogWarning("Cannot extract file {FileName} - source P4K or entry not available", file.FullPath);
                                failedCount++;
                                continue;
                            }

                            // Create output path preserving directory structure
                            var relativePath = file.FullPath.Replace('/', Path.DirectorySeparatorChar);
                            var outputPath = Path.Combine(outputFolder, relativePath);
                            var outputDir = Path.GetDirectoryName(outputPath);
                            if (!string.IsNullOrEmpty(outputDir))
                            {
                                Directory.CreateDirectory(outputDir);
                            }

                            // Extract file
                            using var entryStream = sourceP4k.OpenStream(zipEntry);
                            using var outputStream = File.Create(outputPath);
                            entryStream.CopyTo(outputStream);
                            
                            extractedCount++;
                            
                            _logger.LogDebug("Extracted selected file: {SourcePath} -> {OutputPath}", file.FullPath, outputPath);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to extract selected file: {FileName}", file.FullPath);
                            failedCount++;
                        }
                    }

                    Dispatcher.UIThread.Post(() =>
                    {
                        ComparisonStatus = $"Selected file extraction complete! Extracted: {extractedCount}, Failed: {failedCount}";
                        _logger.LogInformation("Selected P4K file extraction completed - Extracted: {Extracted}, Failed: {Failed}, Output folder: {OutputFolder}", 
                            extractedCount, failedCount, outputFolder);
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to extract selected P4K files");
                    Dispatcher.UIThread.Post(() => ComparisonStatus = $"Selected file extraction failed: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting selected P4K files");
            ComparisonStatus = $"Error extracting selected files: {ex.Message}";
        }
        finally
        {
            IsComparing = false;
        }
    }

    [RelayCommand]
    public async Task ExtractSelectedDataCoreFiles()
    {
        if (!CanExtractSelectedDataCoreFiles)
        {
            _logger.LogWarning("Cannot extract selected DataCore files - no files selected or output directory not configured");
            return;
        }

        try
        {
            var outputFolder = Path.Combine(P4kOutputDirectory, "Selected_DataCore_Files");
            Directory.CreateDirectory(outputFolder);

            IsComparing = true;
            ComparisonStatus = "Extracting selected DataCore files...";

            await Task.Run(() =>
            {
                try
                {
                    var selectedFiles = SelectedDataCoreFiles.OfType<DataCoreComparisonFileNode>().ToArray();
                    var extractedCount = 0;
                    var failedCount = 0;

                    for (int i = 0; i < selectedFiles.Length; i++)
                    {
                        var file = selectedFiles[i];
                        
                        Dispatcher.UIThread.Post(() => 
                            ComparisonStatus = $"Extracting selected DataCore files... {i + 1}/{selectedFiles.Length}");

                        try
                        {
                            // Determine which database to extract from based on file status
                            DataCoreDatabase? sourceDatabase = file.Status switch
                            {
                                DataCoreComparisonStatus.Added => _rightDataCoreDatabase,
                                DataCoreComparisonStatus.Removed => _leftDataCoreDatabase,
                                DataCoreComparisonStatus.Modified => _rightDataCoreDatabase,
                                DataCoreComparisonStatus.Unchanged => _leftDataCoreDatabase,
                                _ => _leftDataCoreDatabase
                            };

                            var record = file.Status switch
                            {
                                DataCoreComparisonStatus.Added => file.RightRecord,
                                DataCoreComparisonStatus.Removed => file.LeftRecord,
                                DataCoreComparisonStatus.Modified => file.RightRecord,
                                DataCoreComparisonStatus.Unchanged => file.LeftRecord ?? file.RightRecord,
                                _ => file.LeftRecord ?? file.RightRecord
                            };

                            if (sourceDatabase == null || record == null)
                            {
                                _logger.LogWarning("Cannot extract DataCore file {FileName} - source database or record not available", file.Name);
                                failedCount++;
                                continue;
                            }

                            // Create output path
                            var fileName = file.Name;
                            if (!fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                            {
                                fileName += ".xml";
                            }
                            var outputPath = Path.Combine(outputFolder, fileName);

                            // Generate XML content for the record
                            var xmlContent = GenerateDataCoreRecordPreview(record.Value, sourceDatabase);
                            
                            // Write to file
                            File.WriteAllText(outputPath, xmlContent);
                            
                            extractedCount++;
                            
                            _logger.LogDebug("Extracted selected DataCore file: {FileName} -> {OutputPath}", file.Name, outputPath);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to extract selected DataCore file: {FileName}", file.Name);
                            failedCount++;
                        }
                    }

                    Dispatcher.UIThread.Post(() =>
                    {
                        ComparisonStatus = $"Selected DataCore file extraction complete! Extracted: {extractedCount}, Failed: {failedCount}";
                        _logger.LogInformation("Selected DataCore file extraction completed - Extracted: {Extracted}, Failed: {Failed}, Output folder: {OutputFolder}", 
                            extractedCount, failedCount, outputFolder);
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to extract selected DataCore files");
                    Dispatcher.UIThread.Post(() => ComparisonStatus = $"Selected DataCore file extraction failed: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting selected DataCore files");
            ComparisonStatus = $"Error extracting selected DataCore files: {ex.Message}";
        }
        finally
        {
            IsComparing = false;
        }
    }

    private static string ExtractVersionFromManifest(string p4kPath)
    {
        if (string.IsNullOrWhiteSpace(p4kPath))
            return "Unknown";

        try
        {
            var directory = Path.GetDirectoryName(p4kPath);
            if (string.IsNullOrWhiteSpace(directory))
                return ExtractVersionFromPath(p4kPath);

            var manifestPath = Path.Combine(directory, "build_manifest.id");
            if (!File.Exists(manifestPath))
                return ExtractVersionFromPath(p4kPath);

            var manifestJson = File.ReadAllText(manifestPath);
            var manifest = JsonSerializer.Deserialize<BuildManifest>(manifestJson);
            
            if (manifest?.Data != null)
            {
                var version = manifest.Data.Version;
                var changeNum = manifest.Data.RequestedP4ChangeNum;
                var branch = manifest.Data.Branch;

                // Clean version to only include major.minor.patch (remove build number)
                var cleanVersion = CleanVersionNumber(version);

                // Build version string in requested format: Version-ChangeNum
                if (!string.IsNullOrWhiteSpace(cleanVersion) && !string.IsNullOrWhiteSpace(changeNum))
                {
                    return $"{cleanVersion}-{changeNum}";
                }
                else if (!string.IsNullOrWhiteSpace(branch) && !string.IsNullOrWhiteSpace(changeNum))
                {
                    return $"{branch}-{changeNum}";
                }
                else if (!string.IsNullOrWhiteSpace(cleanVersion))
                {
                    return cleanVersion;
                }
                else if (!string.IsNullOrWhiteSpace(changeNum))
                {
                    return changeNum;
                }
            }
        }
        catch (Exception)
        {
            // Fall back to path-based extraction if manifest reading fails
        }

        return ExtractVersionFromPath(p4kPath);
    }

    private static string CleanVersionNumber(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return "";
        
        // Split version by dots and take only first 3 parts (major.minor.patch)
        var parts = version.Split('.');
        if (parts.Length >= 3)
        {
            return $"{parts[0]}.{parts[1]}.{parts[2]}";
        }
        else if (parts.Length == 2)
        {
            return $"{parts[0]}.{parts[1]}";
            }
            else
            {
            return parts[0];
        }
    }

    private static string SanitizeForFilename(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "Unknown";

        // Replace invalid filename characters with underscores
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = input;
        
        foreach (var invalidChar in invalidChars)
        {
            sanitized = sanitized.Replace(invalidChar, '_');
        }
        
        // Replace spaces with underscores, but keep dots for version numbers
        sanitized = sanitized.Replace(' ', '_');
        
        return sanitized;
    }

    private static string ExtractVersionFromPath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return "Unknown";

        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var directory = Path.GetDirectoryName(filePath);
        
        // Try to extract version from parent directory (like "3.23.1-LIVE" or "4.0.0-PTU")
        if (!string.IsNullOrWhiteSpace(directory))
        {
            var parentDir = Path.GetFileName(directory);
            var versionPatterns = new[]
            {
                @"(\d+\.\d+\.\d+[a-z]?)-?(LIVE|PTU|EPTU)?",  // Matches 4.1.1-LIVE, 3.23.2a-PTU
                @"(\d+\.\d+)-?(LIVE|PTU|EPTU)?",             // Matches 4.1-LIVE, 3.23-PTU
            };
            
            foreach (var pattern in versionPatterns)
            {
                var match = System.Text.RegularExpressions.Regex.Match(parentDir, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var version = match.Groups[1].Value;
                    var channel = match.Groups[2].Value;
                    return string.IsNullOrEmpty(channel) ? version : $"{version}-{channel}";
                }
            }
        }
        
        // Try to extract version patterns from filename like "4.1.1", "3.23.2a", etc.
        var fileVersionPatterns = new[]
        {
            @"(\d+\.\d+\.\d+[a-z]?)",  // Matches 4.1.1, 3.23.2a
            @"(\d+\.\d+)",             // Matches 4.1, 3.23
        };

        foreach (var pattern in fileVersionPatterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(fileName, pattern);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
        }

        // If no version pattern found, try to extract meaningful parts from filename
        // Remove common prefixes/suffixes
        var cleanName = fileName
            .Replace("StarCitizen", "", StringComparison.OrdinalIgnoreCase)
            .Replace("Data", "", StringComparison.OrdinalIgnoreCase)
            .Replace(".p4k", "", StringComparison.OrdinalIgnoreCase)
            .Trim('_', '-', ' ');

        // If we have something meaningful left, use first part
        if (!string.IsNullOrWhiteSpace(cleanName))
        {
            var parts = cleanName.Split(new[] { '_', '-', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0)
            {
                return parts[0];
            }
        }

        // Final fallback - use first 10 characters of filename or "Unknown"
        return fileName.Length > 10 ? fileName.Substring(0, 10) : (string.IsNullOrWhiteSpace(fileName) ? "Unknown" : fileName);
    }

    private static IEnumerable<string> ConsolidateRelatedFiles(IEnumerable<string> filePaths)
    {
        var groups = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var filePath in filePaths)
        {
            var baseKey = GetBaseFileKey(filePath);
            if (!groups.ContainsKey(baseKey))
            {
                groups[baseKey] = new List<string>();
            }
            groups[baseKey].Add(filePath);
        }
        
        foreach (var group in groups.Values)
        {
            // Sort to ensure we get the base file first
            var sortedFiles = group.OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToList();
            yield return GetRepresentativeFile(sortedFiles);
        }
    }
    
    private static string GetBaseFileKey(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        var directory = Path.GetDirectoryName(filePath) ?? "";
        var extension = Path.GetExtension(fileName);
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        
        // Check if this is a DDS file or DDS variant (.dds, .dds.1, .dds.2, etc.)
        var ddsIndex = fileName.LastIndexOf(".dds", StringComparison.OrdinalIgnoreCase);
        if (ddsIndex >= 0)
        {
            // Check if this is a DDS variant (has something after .dds)
            var afterDds = fileName.Substring(ddsIndex + 4); // Everything after ".dds"
            if (string.IsNullOrEmpty(afterDds) || (afterDds.StartsWith(".") && afterDds.Length > 1 && afterDds.Substring(1).All(char.IsDigit)))
            {
                // This is either a base .dds file or a .dds.N variant
                var baseName = fileName.Substring(0, ddsIndex + 4); // Include .dds
                return Path.Combine(directory, baseName);
            }
        }
        
        // Handle model files with LOD suffixes (_lod1, _lod2, etc.)
        var modelExtensions = new[] { ".cga", ".cgam", ".chr", ".skin" };
        if (modelExtensions.Any(ext => extension.Equals(ext, StringComparison.OrdinalIgnoreCase)))
        {
            // Check for _lod[N] pattern
            var lodPattern = System.Text.RegularExpressions.Regex.Match(nameWithoutExtension, @"^(.+)_lod\d+$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (lodPattern.Success)
            {
                var baseName = lodPattern.Groups[1].Value;
                return Path.Combine(directory, baseName + extension);
            }
        }
        
        // Default: return the file as-is for grouping
        return filePath;
    }
    
    private static string GetRepresentativeFile(List<string> relatedFiles)
    {
        if (relatedFiles.Count == 1)
        {
            return relatedFiles[0];
        }
        
        // For DDS files, prefer the base .dds file (without numeric suffix)
        var ddsFiles = relatedFiles.Where(f => f.Contains(".dds", StringComparison.OrdinalIgnoreCase)).ToArray();
        if (ddsFiles.Length > 0)
        {
            // Look for the base .dds file (without .1, .2, etc.)
            var ddsBase = ddsFiles.FirstOrDefault(f => 
            {
                var fileName = Path.GetFileName(f);
                var ddsIndex = fileName.LastIndexOf(".dds", StringComparison.OrdinalIgnoreCase);
                if (ddsIndex >= 0)
                {
                    var afterDds = fileName.Substring(ddsIndex + 4);
                    return string.IsNullOrEmpty(afterDds); // Only base .dds file has nothing after .dds
                }
                return false;
            });
            
            if (ddsBase != null)
            {
                return ddsBase;
            }
            
            // If no base .dds found, return the first DDS file
            return ddsFiles[0];
        }
        
        // For model files, prefer the base file (without _lod suffix)
        var modelExtensions = new[] { ".cga", ".cgam", ".chr", ".skin" };
        foreach (var ext in modelExtensions)
        {
            var modelBase = relatedFiles.FirstOrDefault(f => 
                f.EndsWith(ext, StringComparison.OrdinalIgnoreCase) && 
                !System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileNameWithoutExtension(f), @"_lod\d+$", System.Text.RegularExpressions.RegexOptions.IgnoreCase));
            if (modelBase != null)
            {
                return modelBase;
            }
        }
        
        // Fallback: return the first file
        return relatedFiles[0];
    }

    [RelayCommand]
    public async Task SelectGameFolder()
    {
        try
        {
            var appLifetime = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
            var topLevel = appLifetime?.MainWindow;
            if (topLevel == null) return;

            var folderPickerOptions = new FolderPickerOpenOptions
            {
                Title = "Select Star Citizen game folder",
                AllowMultiple = false
            };

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(folderPickerOptions);
            if (folders.Count > 0)
            {
                GameFolder = folders[0].Path.LocalPath;
                _logger.LogInformation("Game folder selected: {GameFolder}", GameFolder);
                
                // Try to detect channel from the selected folder name and update selection
                var folderName = Path.GetFileName(GameFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                if (folderName == "LIVE" || folderName == "PTU" || folderName == "EPTU")
                {
                    // Update channel selection to match the folder, but don't change the GameFolder 
                    // (since user specifically selected this path)
                    SelectedChannel = folderName;
                    IsLiveSelected = folderName == "LIVE";
                    IsPtuSelected = folderName == "PTU";
                    IsEptuSelected = folderName == "EPTU";
                    _logger.LogInformation("Auto-detected channel: {Channel}", folderName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting game folder");
            AddLogMessage($"Error selecting game folder: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task SelectOutputDirectory()
    {
        try
        {
            var appLifetime = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
            var topLevel = appLifetime?.MainWindow;
            if (topLevel == null) return;

            var folderPickerOptions = new FolderPickerOpenOptions
            {
                Title = "Select output directory",
                AllowMultiple = false
            };

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(folderPickerOptions);
            if (folders.Count > 0)
            {
                OutputDirectory = folders[0].Path.LocalPath;
                _logger.LogInformation("Output directory selected: {OutputDirectory}", OutputDirectory);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting output directory");
            AddLogMessage($"Error selecting output directory: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task SelectP4kOutputDirectory()
    {
        try
        {
            var appLifetime = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
            var topLevel = appLifetime?.MainWindow;
            if (topLevel == null) return;

            var folderPickerOptions = new FolderPickerOpenOptions
            {
                Title = "Select P4K output directory for reports and DDS files",
                AllowMultiple = false
            };

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(folderPickerOptions);
            if (folders.Count > 0)
            {
                P4kOutputDirectory = folders[0].Path.LocalPath;
                _logger.LogInformation("P4K output directory selected: {P4kOutputDirectory}", P4kOutputDirectory);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting P4K output directory");
        }
    }

    [RelayCommand]
    public async Task RunDiff()
    {
        if (IsRunning) return;

        if (string.IsNullOrWhiteSpace(GameFolder) || string.IsNullOrWhiteSpace(OutputDirectory))
        {
            AddLogMessage("Please select both game folder and output directory.");
            return;
        }

        var p4kFile = Path.Combine(GameFolder, "Data.p4k");
        var exeFile = Path.Combine(GameFolder, "Bin64", "StarCitizen.exe");

        if (!File.Exists(p4kFile))
        {
            AddLogMessage($"Data.p4k not found in {GameFolder}");
            return;
        }

        if (!File.Exists(exeFile))
        {
            AddLogMessage($"StarCitizen.exe not found in {Path.Combine(GameFolder, "Bin64")}");
            return;
        }

        IsRunning = true;
        Progress = 0;
        LogMessages.Clear();

        try
        {
            await Task.Run(async () =>
            {
                await ExecuteDiffCommand(p4kFile, exeFile);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running diff command");
            AddLogMessage($"Error: {ex.Message}");
        }
        finally
        {
            Dispatcher.UIThread.Post(() =>
            {
                IsRunning = false;
                CurrentStatus = "Completed";
                Progress = 1.0;
            });
        }
    }

    private async Task ExecuteDiffCommand(string p4kFile, string exeFile)
    {
        var totalSteps = 7;
        var currentStep = 0;

        // Helper to update progress with both step and sub-step progress
        void UpdateStepProgress(int step, double subProgress, string status)
        {
            var overallProgress = (step - 1 + subProgress) / totalSteps;
            UpdateProgress(overallProgress, status);
        }

        // Step 1: Clean old files
        currentStep = 1;
        if (!KeepOldFiles)
        {
            UpdateStepProgress(currentStep, 0.0, "Deleting old files...");
            await CleanOldFiles((progress) => UpdateStepProgress(currentStep, progress, "Deleting old files..."));
        }
        else
        {
            UpdateStepProgress(currentStep, 1.0, "Keeping old files...");
        }

        // Step 2: Dump P4k structure
        currentStep = 2;
        UpdateStepProgress(currentStep, 0.0, "Dumping P4k structure...");
        await DumpP4kStructure(p4kFile, (progress) => UpdateStepProgress(currentStep, progress, "Dumping P4k structure..."));

        // Step 3: Extract localization
        currentStep = 3;
        UpdateStepProgress(currentStep, 0.0, "Extracting localization...");
        await ExtractLocalization(p4kFile, (progress) => UpdateStepProgress(currentStep, progress, "Extracting localization..."));

        // Step 4: Extract DataCore
        currentStep = 4;
        UpdateStepProgress(currentStep, 0.0, "Extracting DataCore...");
        await ExtractDataCore(p4kFile, (progress) => UpdateStepProgress(currentStep, progress, "Extracting DataCore..."));

        // Step 5: Extract Protobuf data
        currentStep = 5;
        UpdateStepProgress(currentStep, 0.0, "Extracting Protobuf data...");
        await ExtractProtobufData(exeFile, (progress) => UpdateStepProgress(currentStep, progress, "Extracting Protobuf data..."));

        // Step 6: Create compressed archives
        currentStep = 6;
        UpdateStepProgress(currentStep, 0.0, "Creating compressed archives...");
        await CreateCompressedArchives(p4kFile, exeFile, (progress) => UpdateStepProgress(currentStep, progress, "Creating compressed archives..."));

        // Step 7: Copy build manifest
        currentStep = 7;
        UpdateStepProgress(currentStep, 0.0, "Copying build manifest...");
        CopyBuildManifest((progress) => UpdateStepProgress(currentStep, progress, "Copying build manifest..."));

        UpdateProgress(1.0, "Diff operation completed!");
    }

    private async Task CleanOldFiles(Action<double> progressCallback)
    {
        try
        {
            string[] deleteDirectories =
            [
                Path.Combine(OutputDirectory, "DataCore"),
                Path.Combine(OutputDirectory, "P4k"),
                Path.Combine(OutputDirectory, "P4kContents"),
                Path.Combine(OutputDirectory, "Localization"),
                Path.Combine(OutputDirectory, "Protobuf"),
            ];

            string[] deleteFiles =
            [
                Path.Combine(OutputDirectory, "build_manifest.json"),
                Path.Combine(OutputDirectory, "DataCore.dcb.zst"),
                Path.Combine(OutputDirectory, "StarCitizen.exe.zst"),
            ];

            var totalSteps = deleteDirectories.Length + deleteFiles.Length;
            var currentStep = 0;

            await Task.Run(() =>
            {
            foreach (var dir in deleteDirectories)
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, true);
                    AddLogMessage($"Deleted directory: {Path.GetFileName(dir)}");
                }
                    progressCallback(++currentStep / (double)totalSteps);
            }

            foreach (var file in deleteFiles)
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                    AddLogMessage($"Deleted file: {Path.GetFileName(file)}");
                }
                    progressCallback(++currentStep / (double)totalSteps);
            }
            });

            AddLogMessage("Old files cleaned up.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning old files");
            AddLogMessage($"Error cleaning old files: {ex.Message}");
            progressCallback(1.0);
        }
    }

    private async Task DumpP4kStructure(string p4kFile, Action<double> progressCallback)
    {
        try
        {
            await Task.Run(() =>
            {
                var p4k = P4kFile.FromFile(p4kFile);
                // Create the directory node from the P4k file
                var rootNode = P4kDirectoryNode.FromP4k(p4k);
                var outputDir = Path.Combine(OutputDirectory, "P4k");
                
                // Count total nodes for progress tracking
                var totalNodes = CountNodes(rootNode);
                var processedNodes = 0;
                var lastReportedProgress = 0.0;
                
                WriteFileForNode(outputDir, rootNode, () =>
                {
                    processedNodes++;
                    var currentProgress = (double)processedNodes / totalNodes;
                    
                    // Only update progress if it changed by at least 1% or we're done
                    if (currentProgress - lastReportedProgress >= 0.01 || currentProgress >= 1.0)
                    {
                        progressCallback(currentProgress);
                        lastReportedProgress = currentProgress;
                    }
                });
            });
            AddLogMessage("P4k structure dumped successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dumping P4k structure");
            AddLogMessage($"Error dumping P4k structure: {ex.Message}");
        }
    }

    private async Task ExtractLocalization(string p4kFile, Action<double> progressCallback)
    {
        try
        {
            await Task.Run(() =>
            {
                progressCallback(0.1);
                
                // Implementation for extracting localization files
                // This would extract global.ini files from the P4k
                var p4kFileSystem = new P4kFileSystem(P4kFile.FromFile(p4kFile));
                var outputDir = Path.Combine(OutputDirectory, "Localization");
                
                progressCallback(0.3);
                
                // Look for localization files
                string[] localizationPaths = [
                    "Data/Localization/english/global.ini",
                    "Data\\Localization\\english\\global.ini"
                ];

                progressCallback(0.5);

                foreach (var path in localizationPaths)
                {
                    if (p4kFileSystem.FileExists(path))
                    {
                        progressCallback(0.7);
                        
                        using var stream = p4kFileSystem.OpenRead(path);
                        var outputPath = Path.Combine(outputDir, "global.ini");
                        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                        
                        using var outputStream = File.Create(outputPath);
                        stream.CopyTo(outputStream);
                        
                        progressCallback(1.0);
                        break;
                    }
                }
                
                if (progressCallback != null && Progress < 1.0)
                {
                    progressCallback(1.0);
                }
            });
            AddLogMessage("Localization files extracted.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting localization");
            AddLogMessage($"Error extracting localization: {ex.Message}");
            progressCallback(1.0);
        }
    }

    private async Task ExtractDataCore(string p4kFile, Action<double> progressCallback)
    {
        try
        {
            await Task.Run(() =>
            {
                progressCallback(0.1);
                
                var p4kFileSystem = new P4kFileSystem(P4kFile.FromFile(p4kFile));
                
                progressCallback(0.2);
                
                Stream? dcbStream = null;
                foreach (var file in DataCoreUtils.KnownPaths)
                {
                    if (!p4kFileSystem.FileExists(file)) continue;
                    dcbStream = p4kFileSystem.OpenRead(file);
                    break;
                }

                if (dcbStream == null)
                {
                    AddLogMessage("DataCore not found in P4k file.");
                    progressCallback(1.0);
                    return;
                }

                progressCallback(0.3);

                var outputDir = Path.Combine(OutputDirectory, "DataCore");
                var df = TextFormat switch
                {
                    "json" => DataForge.FromDcbStreamJson(dcbStream),
                    _ => DataForge.FromDcbStreamXml(dcbStream),
                };

                progressCallback(0.4);

                // Create a simple progress reporter that doesn't overwhelm the UI
                var lastReportedProgress = 0.4;
                var progress = new Progress<double>(value => 
                {
                    var mappedProgress = 0.4 + (value * 0.6);
                    
                    // Only update if progress changed by at least 2% or we're done
                    if (mappedProgress - lastReportedProgress >= 0.02 || value >= 1.0)
                    {
                        progressCallback(mappedProgress);
                        lastReportedProgress = mappedProgress;
                        
                        // Only log every 10% to avoid flooding the log
                        if (value % 0.1 < 0.02 || value >= 1.0)
                {
                    AddLogMessage($"DataCore extraction progress: {value:P0}");
                        }
                    }
                });
                
                df.ExtractAllParallel(outputDir, null, progress);
                progressCallback(1.0);
            });
            AddLogMessage("DataCore extracted successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting DataCore");
            AddLogMessage($"Error extracting DataCore: {ex.Message}");
            progressCallback(1.0);
        }
    }

    private async Task ExtractProtobufData(string exeFile, Action<double> progressCallback)
    {
        try
        {
            await Task.Run(() =>
            {
                progressCallback(0.1);

                var outputDir = Path.Combine(OutputDirectory, "Protobuf");
                Directory.CreateDirectory(outputDir);

                var extractor = ProtobufExtractor.FromFilename(exeFile);
                progressCallback(0.3);

                extractor.WriteProtos(outputDir, p => !p.Name.StartsWith("google/protobuf"));
                progressCallback(0.7);

                var descriptorPath = Path.Combine(outputDir, "descriptor_set.bin");
                extractor.WriteDescriptorSet(descriptorPath);
                progressCallback(1.0);
            });

            AddLogMessage("Protobuf definitions and descriptor set extracted.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting protobuf data");
            AddLogMessage($"Error extracting protobuf data: {ex.Message}");
            progressCallback(1.0);
        }
    }

    private async Task CreateCompressedArchives(string p4kFile, string exeFile, Action<double> progressCallback)
    {
        try
        {
            await Task.Run(async () =>
            {
                // Extract DataCore into compressed file (50% of this step)
                await ExtractDataCoreIntoZip(p4kFile, Path.Combine(OutputDirectory, "DataCore.dcb.zst"), 
                    (progress) => progressCallback(progress * 0.5));
                
                // Extract executable into compressed file (remaining 50% of this step)
                await ExtractExecutableIntoZip(exeFile, Path.Combine(OutputDirectory, "StarCitizen.exe.zst"), 
                    (progress) => progressCallback(0.5 + (progress * 0.5)));
            });
            AddLogMessage("Compressed archives created.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating compressed archives");
            AddLogMessage($"Error creating compressed archives: {ex.Message}");
            progressCallback(1.0);
        }
    }

    private void CopyBuildManifest(Action<double> progressCallback)
    {
        try
        {
            progressCallback(0.2);
            
            var buildManifestSource = Path.Combine(GameFolder, "build_manifest.id");
            var buildManifestTarget = Path.Combine(OutputDirectory, "build_manifest.json");
            
            progressCallback(0.5);
            
            if (File.Exists(buildManifestSource))
            {
                File.Copy(buildManifestSource, buildManifestTarget, true);
                AddLogMessage("Build manifest copied.");
            }
            else
            {
                AddLogMessage("Build manifest not found.");
            }
            
            progressCallback(1.0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error copying build manifest");
            AddLogMessage($"Error copying build manifest: {ex.Message}");
            progressCallback(1.0);
        }
    }

    // Helper methods from CLI commands
    private static void WriteFileForNode(string baseDir, P4kDirectoryNode directoryNode, Action? progressCallback = null)
    {
        var dir = new System.Xml.Linq.XElement("Directory",
            new System.Xml.Linq.XAttribute("Name", directoryNode.Name)
        );

        foreach (var (_, childNode) in directoryNode.Children.OrderBy(x => x.Key))
        {
            switch (childNode)
            {
                case P4kDirectoryNode childDirectoryNode:
                    WriteFileForNode(Path.Combine(baseDir, childDirectoryNode.Name), childDirectoryNode, progressCallback);
                    break;
                case P4kFileNode childFileNode:
                    dir.Add(new System.Xml.Linq.XElement("File",
                        new System.Xml.Linq.XAttribute("Name", Path.GetFileName(childFileNode.P4KEntry.Name)),
                        new System.Xml.Linq.XAttribute("CRC32", $"0x{childFileNode.P4KEntry.Crc32:X8}"),
                        new System.Xml.Linq.XAttribute("Size", childFileNode.P4KEntry.UncompressedSize.ToString()),
                        new System.Xml.Linq.XAttribute("CompressionType", childFileNode.P4KEntry.CompressionMethod.ToString()),
                        new System.Xml.Linq.XAttribute("Encrypted", childFileNode.P4KEntry.IsCrypted.ToString())
                    ));
                    break;
            }
        }

        if (dir.HasElements)
        {
            var filePath = Path.Combine(baseDir, directoryNode.Name) + ".xml";
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            dir.Save(filePath);
        }

        progressCallback?.Invoke();
    }

    private static int CountNodes(P4kDirectoryNode directoryNode)
    {
        var count = 1; // Count this directory
        foreach (var (_, childNode) in directoryNode.Children)
        {
            if (childNode is P4kDirectoryNode childDir)
            {
                count += CountNodes(childDir);
            }
            else
            {
                count++; // Count file nodes
            }
        }
        return count;
    }

    private static async Task ExtractDataCoreIntoZip(string p4kFile, string zipPath, Action<double> progressCallback)
    {
        var p4k = new P4kFileSystem(P4kFile.FromFile(p4kFile));
        Stream? input = null;
        foreach (var file in DataCoreUtils.KnownPaths)
        {
            if (!p4k.FileExists(file)) continue;
            input = p4k.OpenRead(file);
            break;
        }

        if (input == null)
            throw new InvalidOperationException("DataCore not found.");

        progressCallback(0.1);

        Directory.CreateDirectory(Path.GetDirectoryName(zipPath)!);
        await using var output = File.OpenWrite(zipPath);
        await using var compressionStream = new CompressionStream(output, leaveOpen: false);
        
        progressCallback(0.3);
        
        // Copy with throttled progress tracking
        var buffer = new byte[81920]; // Larger buffer
        var totalBytesRead = 0L;
        var inputLength = input.Length;
        var lastReportedProgress = 0.0;
        int bytesRead;
        
        while ((bytesRead = await input.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            await compressionStream.WriteAsync(buffer, 0, bytesRead);
            totalBytesRead += bytesRead;
            
            if (inputLength > 0)
            {
                var copyProgress = (double)totalBytesRead / inputLength;
                var currentProgress = 0.3 + (copyProgress * 0.7);
                
                // Only update progress if it changed by at least 1%
                if (currentProgress - lastReportedProgress >= 0.01 || copyProgress >= 1.0)
                {
                    progressCallback(currentProgress);
                    lastReportedProgress = currentProgress;
                }
            }
        }

        progressCallback(1.0);
    }

    private static async Task ExtractExecutableIntoZip(string exeFile, string zipPath, Action<double> progressCallback)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(zipPath)!);
        await using var input = File.OpenRead(exeFile);
        await using var output = File.OpenWrite(zipPath);
        await using var compressionStream = new CompressionStream(output, leaveOpen: false);
        
        progressCallback(0.1);
        
        // Copy with throttled progress tracking
        var buffer = new byte[81920]; // Larger buffer
        var totalBytesRead = 0L;
        var inputLength = input.Length;
        var lastReportedProgress = 0.0;
        int bytesRead;
        
        while ((bytesRead = await input.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            await compressionStream.WriteAsync(buffer, 0, bytesRead);
            totalBytesRead += bytesRead;
            
            var copyProgress = (double)totalBytesRead / inputLength;
            var currentProgress = 0.1 + (copyProgress * 0.9);
            
            // Only update progress if it changed by at least 1%
            if (currentProgress - lastReportedProgress >= 0.01 || copyProgress >= 1.0)
            {
                progressCallback(currentProgress);
                lastReportedProgress = currentProgress;
            }
        }

        progressCallback(1.0);
    }

    private void UpdateProgress(double progress, string status)
    {
        Dispatcher.UIThread.Post(() =>
        {
            Progress = progress;
            CurrentStatus = status;
            AddLogMessage(status);
        });
    }

    private void AddLogMessage(string message)
    {
        var formattedMessage = $"[{DateTime.Now:HH:mm:ss}] {message}";
        
        // Ensure UI updates are always dispatched to the UI thread
        if (Dispatcher.UIThread.CheckAccess())
        {
            LogMessages.Add(formattedMessage);
            _logger.LogInformation(message);
        }
        else
        {
            Dispatcher.UIThread.Post(() =>
            {
                LogMessages.Add(formattedMessage);
                _logger.LogInformation(message);
            });
        }
    }

    // Helper method to throttle progress updates
    private class ProgressThrottler
    {
        private double _lastReportedProgress;
        private readonly double _threshold;
        private readonly Action<double> _callback;

        public ProgressThrottler(Action<double> callback, double threshold = 0.01)
        {
            _callback = callback;
            _threshold = threshold;
        }

        public void Report(double progress)
        {
            if (progress - _lastReportedProgress >= _threshold || progress >= 1.0 || progress <= 0.0)
            {
                _callback(progress);
                _lastReportedProgress = progress;
            }
        }
    }

    // Helper to open a nested entry stream with proper SOC/PAK resolution
    private Stream OpenEntryStream(P4kComparisonFileNode fileNode, bool useLeft)
    {
        var sourceP4k = useLeft ? _leftP4kFile : _rightP4kFile;
        var zipEntry = useLeft ? fileNode.LeftEntry : fileNode.RightEntry;
        if (sourceP4k == null || zipEntry == null)
            throw new InvalidOperationException("P4k file or entry is null in OpenEntryStream");

        var contextP4k = sourceP4k;
        var entryToOpen = zipEntry;
        var fullPathParts = fileNode.FullPath.Split('\\');
        for (int i = 0; i < fullPathParts.Length - 1; i++)
        {
            var part = fullPathParts[i];
            if ((part.EndsWith(".socpak", StringComparison.OrdinalIgnoreCase) || part.EndsWith(".pak", StringComparison.OrdinalIgnoreCase))
                && !part.Contains("shadercache_", StringComparison.OrdinalIgnoreCase))
            {
                var archiveEntry = contextP4k.Entries.FirstOrDefault(e => Path.GetFileName(e.Name)
                    .Equals(part, StringComparison.OrdinalIgnoreCase));
                if (archiveEntry != null)
                {
                    contextP4k = P4kFile.FromP4kEntry(contextP4k, archiveEntry);
                    var nestedEntry = contextP4k.Entries.FirstOrDefault(e => Path.GetFileName(e.Name)
                        .Equals(fileNode.Name, StringComparison.OrdinalIgnoreCase));
                    if (nestedEntry != null)
                        entryToOpen = nestedEntry;
                }
                break;
            }
        }
        try
        {
            return contextP4k.OpenStream(entryToOpen);
        }
        catch (Exception ex) when (ex.Message.Contains("Invalid local file header"))
        {
            _logger.LogWarning(ex, "Nested archive stream failed in OpenEntryStream, falling back for {FilePath}", fileNode.FullPath);
            return sourceP4k.OpenStream(zipEntry);
        }
    }
} 