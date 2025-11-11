using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Selection;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using StarBreaker.DataCore;
using StarBreaker.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Avalonia.Platform.Storage;

namespace StarBreaker.Screens;

public sealed partial class DataCoreTabViewModel : PageViewModelBase
{
    private const string dataCorePath = "Data\\Game2.dcb";
    public override string Name => "DataCore";
    public override string Icon => "ViewAll";

    private readonly IP4kService _p4KService;
    private readonly ITagDatabaseService _tagDatabaseService;
    private readonly ILogger<DataCoreTabViewModel> _logger;

    public DataCoreTabViewModel(IP4kService p4kService, ITagDatabaseService tagDatabaseService, ILogger<DataCoreTabViewModel> logger)
    {
        _p4KService = p4kService;
        _tagDatabaseService = tagDatabaseService;
        _logger = logger;
        
        Records = new HierarchicalTreeDataGridSource<DataCoreRecordViewModel>(Array.Empty<DataCoreRecordViewModel>())
        {
            Columns =
            {
                new HierarchicalExpanderColumn<DataCoreRecordViewModel>(
                    new TextColumn<DataCoreRecordViewModel, string>("Name", x => x.Name),
                    x => x.Children?.ToArray() ?? Array.Empty<DataCoreRecordViewModel>()
                ),
                new TextColumn<DataCoreRecordViewModel, string>("Type", x => x.Type),
                new TextColumn<DataCoreRecordViewModel, string>("ID", x => x.Id),
            }
        };
        
        Records.RowSelection!.SingleSelect = true;
        Records.RowSelection.SelectionChanged += SelectionChanged;
        
        SearchText = string.Empty;

        Task.Run(Initialize);
    }

    [RelayCommand]
    private async Task ExtractSelectedRecord()
    {
        try
        {
            var selected = Records.RowSelection?.SelectedItem;
            if (selected == null || selected.IsFolder || selected.Record == null || DataCore == null)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ErrorMessage = "Select a single record to extract.";
                });
                return;
            }

            var appLifetime = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
            var topLevel = appLifetime?.MainWindow;
            if (topLevel == null)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ErrorMessage = "Unable to open folder picker.";
                });
                return;
            }

            var folderPickerOptions = new FolderPickerOpenOptions
            {
                Title = "Select destination folder",
                AllowMultiple = false
            };

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(folderPickerOptions);
            if (folders == null || folders.Count == 0)
                return; // user canceled

            var destinationPath = folders[0].Path.LocalPath;

            var record = selected.Record.Value;
            var fileName = record.GetFileName(DataCore.Database);
            var outputPath = Path.Combine(destinationPath, fileName);
            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir))
                Directory.CreateDirectory(outputDir);

            DataCore.SaveRecordToFile(record, outputPath);
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ErrorMessage = ex.Message;
            });
        }
    }
    
    private async void Initialize()
    {
        try
        {
            var entry = _p4KService.P4KFileSystem.OpenRead(dataCorePath);
            var dcb = new DataCoreDatabase(entry);
            entry.Dispose();
            
            var rootRecords = new List<DataCoreRecordViewModel>();
            var pathRecords = new Dictionary<string, DataCoreRecordViewModel>();
            
            // Create folders for each unique path
            foreach (var recordId in dcb.MainRecords)
            {
                var record = dcb.GetRecord(recordId);
                var filename = record.GetFileName(dcb);
                var directory = Path.GetDirectoryName(filename) ?? "";
                
                // Create folder hierarchy
                var currentPath = "";
                var currentList = rootRecords;
                
                foreach (var part in directory.Split('\\', StringSplitOptions.RemoveEmptyEntries))
                {
                    currentPath = string.IsNullOrEmpty(currentPath) ? part : Path.Combine(currentPath, part);
                    
                    if (!pathRecords.TryGetValue(currentPath, out var folder))
                    {
                        folder = new DataCoreRecordViewModel(part);
                        pathRecords[currentPath] = folder;
                        currentList.Add(folder);
                    }
                    
                    currentList = folder.Children!;
                }
                
                // Add record to final folder
                var recordName = Path.GetFileName(filename);
                var recordViewModel = new DataCoreRecordViewModel(record, dcb, recordName);
                currentList.Add(recordViewModel);
            }
            
            // Sort all folders and records
            SortRecordsRecursive(rootRecords);
            
            // Store the complete record set for searching
            _allRecords = rootRecords;
            
            await Dispatcher.UIThread.InvokeAsync(() => 
            {
                DataCore = new DataCoreBinaryXml(dcb);
                Records.Items = rootRecords;
                IsLoading = false;
            });
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() => 
            {
                ErrorMessage = $"Failed to load DataCore: {ex.Message}";
                IsLoading = false;
            });
        }
    }
    
    private void SortRecordsRecursive(List<DataCoreRecordViewModel> records)
    {
        // Sort: folders first, then by name
        records.Sort((a, b) => 
        {
            if (a.IsFolder && !b.IsFolder) return -1;
            if (!a.IsFolder && b.IsFolder) return 1;
            return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
        });
        
        // Sort children recursively
        foreach (var record in records.Where(r => r.IsFolder))
        {
            if (record.Children != null)
                SortRecordsRecursive(record.Children);
        }
    }

    private void SelectionChanged(object? sender, TreeSelectionModelSelectionChangedEventArgs<DataCoreRecordViewModel> e)
    {
        if (e.SelectedItems.Count != 1 || DataCore == null)
            return;

        var selectedRecord = e.SelectedItems[0];
        if (selectedRecord == null || selectedRecord.IsFolder || selectedRecord.Record == null)
        {
            SelectedRecordContent = null;
            return;
        }

        // Get record XML
        try 
        {
            var content = DataCore.GetFromMainRecord(selectedRecord.Record ?? throw new InvalidOperationException("Record is null"));
            content = ResolveXmlTags(content);
            SelectedRecordContent = content;
        }
        catch (Exception ex)
        {
            SelectedRecordContent = $"Error loading record: {ex.Message}";
        }
    }

    private void ApplySearch()
    {
        if (_allRecords == null)
            return;

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            Records.Items = _allRecords;
            return;
        }

        var searchTerm = SearchText.Trim().ToLowerInvariant();
        var filteredRecords = CloneAndFilterRecords(_allRecords, searchTerm);
        Records.Items = filteredRecords;
    }

    private List<DataCoreRecordViewModel> CloneAndFilterRecords(List<DataCoreRecordViewModel> records, string searchTerm)
    {
        var result = new List<DataCoreRecordViewModel>();

        foreach (var record in records)
        {
            bool matches = record.Name.ToLowerInvariant().Contains(searchTerm);

            if (record.IsFolder)
            {
                var filteredChildren = record.Children != null
                    ? CloneAndFilterRecords(record.Children, searchTerm)
                    : new List<DataCoreRecordViewModel>();

                if (matches || filteredChildren.Count > 0)
                {
                    var folderClone = new DataCoreRecordViewModel(record.Name, filteredChildren);
                    result.Add(folderClone);
                }
            }
            else if (matches)
            {
                result.Add(record);
            }
        }

        return result;
    }
 
    [ObservableProperty] private DataCoreBinaryXml? _dataCore;
    
    [ObservableProperty] private HierarchicalTreeDataGridSource<DataCoreRecordViewModel> _records;
    
    [ObservableProperty] private string? _selectedRecordContent;
    
    [ObservableProperty] private bool _isLoading = true;
    
    [ObservableProperty] private string? _errorMessage;
    
    [ObservableProperty] private string _searchText = string.Empty;
    
    private List<DataCoreRecordViewModel>? _allRecords;
    
    partial void OnSearchTextChanged(string value)
    {
        ApplySearch();
    }
    
    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = string.Empty;
    }
    
    private string ResolveXmlTags(string xml)
    {
        try
        {
            // Use regex to find all Tag/tag RecordId attributes and replace them with comments showing the tag name
            var regex = new Regex(
                @"<[Tt]ag[^>]*RecordId=""([a-fA-F0-9\-]+)""[^>]*/?>",
                RegexOptions.IgnoreCase);

            return regex.Replace(xml, match =>
            {
                var recordId = match.Groups[1].Value;
                var tagName = _tagDatabaseService.ResolveTagName(recordId);
                
                if (tagName != null)
                {
                    // Add a comment before the Tag element showing the tag name
                    return $"<!-- Tag: {tagName} -->{match.Value}";
                }
                
                return match.Value;
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve XML tags");
            return xml;
        }
    }
}

public class DataCoreRecordViewModel : ViewModelBase
{
    // For records
    public DataCoreRecordViewModel(DataCoreRecord record, DataCoreDatabase database, string name)
    {
        Record = record;
        Name = name;
        Id = record.Id.ToString();
        Type = database.StructDefinitions[record.StructIndex].GetName(database);
        IsFolder = false;
        Children = null;
    }
    
    // For folders
    public DataCoreRecordViewModel(string folderName)
    {
        Name = folderName;
        Id = string.Empty;
        Type = "Folder";
        IsFolder = true;
        Children = new List<DataCoreRecordViewModel>();
    }
    
    // Copy constructor for folders with filtered children
    public DataCoreRecordViewModel(string folderName, List<DataCoreRecordViewModel> children)
    {
        Name = folderName;
        Id = string.Empty;
        Type = "Folder";
        IsFolder = true;
        Children = children;
    }
    
    public DataCoreRecord? Record { get; }
    public string Name { get; }
    public string Id { get; }
    public string Type { get; }
    public bool IsFolder { get; }
    public List<DataCoreRecordViewModel>? Children { get; }
}