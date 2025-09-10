using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Selection;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StarBreaker.DataCore;
using StarBreaker.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace StarBreaker.Screens;

public sealed partial class DataCoreTabViewModel : PageViewModelBase
{
    private const string dataCorePath = "Data\\Game2.dcb";
    public override string Name => "DataCore";
    public override string Icon => "ViewAll";

    private readonly IP4kService _p4KService;

    public DataCoreTabViewModel(IP4kService p4kService)
    {
        _p4KService = p4kService;
        
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
    private Task ApplyOwnedGuid()
        => ApplyGuidToStreamScriptInternal("owned");

    [RelayCommand]
    private Task ApplyDesiredGuid()
        => ApplyGuidToStreamScriptInternal("desired");

    private async Task ApplyGuidToStreamScriptInternal(string mode)
    {
        try
        {
            // Auto-detect variable name from selected content and mode
            var effectiveVar = InferVarNameFromContent(SelectedRecordContent);
            if (string.IsNullOrWhiteSpace(effectiveVar))
                throw new InvalidOperationException("Could not auto-detect target variable. Select a record that identifies a ship/armor/weapon/paint.");

            // Map mode to stream variable families
            // owned: map to the "...2" variants if present (paint2, sidearm2, etc.) else family 1
            // desired: map to the primary family 1 (idris, paint1, etc.) we want to receive
            // owned -> family 1, desired -> family 2
            effectiveVar = mode == "owned" ? PromoteToFamily(effectiveVar, 1) : PromoteToFamily(effectiveVar, 2);

            // Locate scripts/stream-sc.py relative to app root
            var root = AppContext.BaseDirectory;
            // Go up until we find scripts folder (dev-time); fallback to current dir
            var probe = root;
            for (int i = 0; i < 5 && !Directory.Exists(Path.Combine(probe, "scripts")); i++)
                probe = Path.GetFullPath(Path.Combine(probe, ".."));
            var scriptsDir = Directory.Exists(Path.Combine(probe, "scripts"))
                ? Path.Combine(probe, "scripts")
                : Path.Combine(Environment.CurrentDirectory, "scripts");

            var scriptPath = Path.Combine(scriptsDir, "stream-sc.py");
            if (!File.Exists(scriptPath))
                throw new FileNotFoundException("stream-sc.py not found", scriptPath);

            var text = await File.ReadAllTextAsync(scriptPath);

            // Build a regex to find a line like: varName = "<guid>"
            // e.g.: idris = "f0caa993-4c6b-4402-8ef1-d91879060f3b"
            var safeName = Regex.Escape(effectiveVar);
            // avoid verbatim+interpolation escaping issues
            var pattern = "^\\s*" + safeName + "\\s*=\\s*\"(?<guid>[0-9a-fA-F\\-]{36})\"\\s*$";
            var regex = new Regex(pattern, RegexOptions.Multiline);
            var match = regex.Match(text);
            if (!match.Success)
                throw new InvalidOperationException($"Variable '{effectiveVar}' not found in stream-sc.py");

            var currentGuid = match.Groups["guid"].Value;

            // Get selected record GUID from DataCore selection if available
            var selected = SelectedRecordContent;
            string? newGuid = null;
            if (!string.IsNullOrWhiteSpace(selected))
            {
                // Look for standard GUID patterns in the XML/JSON content
                var guidMatch = Regex.Match(selected, "[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");
                if (guidMatch.Success) newGuid = guidMatch.Value;
            }

            if (string.IsNullOrWhiteSpace(newGuid))
                throw new InvalidOperationException("No GUID found in selected record. Select a record that contains a GUID.");

            if (string.Equals(currentGuid, newGuid, StringComparison.OrdinalIgnoreCase))
                return; // already set

            // Replace in file
            // Only replace the specific variable's GUID; keep other vars unchanged
            var updated = regex.Replace(text, m => m.Value.Replace(currentGuid, newGuid), 1);
            await File.WriteAllTextAsync(scriptPath, updated);
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ErrorMessage = ex.Message;
            });
        }
    }

    private static string PromoteToFamily(string baseVar, int family)
    {
        // normalize to family 1 name
        string Normalize(string v)
            => v.Replace("2", "1");

        var norm = Normalize(baseVar);
        if (family == 1) return norm;
        // ship/base variables without trailing digit should keep as-is for family 1,
        // for family 2, add/replace with 2 when the script contains that var; otherwise keep original
        if (norm.EndsWith("1")) return norm[..^1] + "2";
        // paint / sidearm / meele names may exist as pair 1/2; append 2 if needed
        return norm + "2";
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

    private static string? InferVarNameFromContent(string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;
        var lower = content.ToLowerInvariant();
        // helpers
        bool HasWord(string w) => Regex.IsMatch(lower, @"\b" + Regex.Escape(w) + @"\b", RegexOptions.IgnoreCase);
        bool HasAny(params string[] words) => words.Any(HasWord);
        bool HasTokenLike(string token)
            => HasWord(token) || lower.Contains(token + "_") || lower.Contains("/" + token) || lower.Contains("\\" + token);

        // 1) Paints (explicit first to avoid ship path tokens like Objects/Spaceships/Paints/...)
        if (lower.Contains("<type>paints</type>") || lower.Contains("/paints/") || lower.Contains("\\paints\\")
            || HasAny("paints", "paint", "livery") || lower.Contains("@item_typepaints"))
            return "paint1";

        // 2) Ships/vehicles
        if (HasAny("spaceship", "spaceships", "spacecraft", "ship", "vessel", "vehicle"))
            return "idris";

        // 3) Weapons (detect early to avoid incidental 'arm' hits)
        if (HasAny("weapon", "weapons") || lower.Contains("/weapons/") || lower.Contains("\\weapons\\")
            || lower.Contains("<type>weaponpersonal</type>"))
        {
            // melee first
            if (HasAny("melee", "meele", "knife", "blade", "sword", "axe", "dagger", "machete")
                || lower.Contains("<subtype>knife</subtype>")
                || lower.Contains("<tags>knife</tags>"))
                return "meele1";

            if (HasAny("sidearm", "pistol")) return "sidearm1";
            return "fpsWeapon1";
        }

        // 4) Armor family (only if clearly in armor context)
        if (HasAny("armor", "armour") || lower.Contains("/armor/") || lower.Contains("\\armor\\"))
        {
            if (HasTokenLike("helmet")) return "armorHelmet1";
            if (HasTokenLike("core") || HasTokenLike("torso")) return "armorCore1";
            if (HasTokenLike("legs") || HasTokenLike("leg")) return "armorLegs1";
            // arms: only explicit arms/forearm/gauntlet/glove (do NOT match bare 'arm' to avoid '/armor/')
            if (HasTokenLike("arms") || lower.Contains("forearm") || lower.Contains("gauntlet") || lower.Contains("glove"))
                return "armorArms1";
        }

        // 5) Modules
        if ((HasWord("module") || lower.Contains("/modules/") || lower.Contains("\\modules\\")) && HasWord("attach"))
            return "moduleBase1";

        return null;
    }
    
    private void ApplySearch()
    {
        if (_allRecords == null)
            return;
            
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            // Reset to show all records
            Records.Items = _allRecords;
            return;
        }
        
        var searchTerm = SearchText.Trim().ToLowerInvariant();
        
        // Clone the hierarchy but only include matches and their parent folders
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
                // For folders, we also check their children
                var filteredChildren = record.Children != null 
                    ? CloneAndFilterRecords(record.Children, searchTerm) 
                    : new List<DataCoreRecordViewModel>();
                
                // Include this folder if it matches or has matching children
                if (matches || filteredChildren.Count > 0)
                {
                    // Create a folder with only the matching children
                    var folderClone = new DataCoreRecordViewModel(record.Name, filteredChildren);
                    result.Add(folderClone);
                }
            }
            else
            {
                // For files, include if it matches
                if (matches)
                {
                    result.Add(record);
                }
            }
        }
        
        return result;
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
            SelectedRecordContent = content;
        }
        catch (Exception ex)
        {
            SelectedRecordContent = $"Error loading record: {ex.Message}";
        }
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