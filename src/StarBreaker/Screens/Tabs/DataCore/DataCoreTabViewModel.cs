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
    private Task ApplyOwnedGuid()
        => ApplyGuidToStreamScriptInternal("owned");

    [RelayCommand]
    private Task ApplyDesiredGuid()
        => ApplyGuidToStreamScriptInternal("desired");

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

    private async Task ApplyGuidToStreamScriptInternal(string mode)
    {
        try
        {
            // Auto-detect base variable category from selected content
            var baseVar = InferVarNameFromContent(SelectedRecordContent);
            if (string.IsNullOrWhiteSpace(baseVar))
                throw new InvalidOperationException("Could not auto-detect target variable. Select a record that identifies a ship/armor/weapon/paint.");

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

            // Resolve actual target variable name based on what's present in the script and the selected mode
            var effectiveVar = ResolveTargetVariableName(text, baseVar!, mode);
            if (string.IsNullOrWhiteSpace(effectiveVar))
                throw new InvalidOperationException($"No suitable variable found in stream-sc.py for base '{baseVar}' and mode '{mode}'.");

            // Build a strict regex to find a line like: varName = "<guid>" or '<guid>'
            var safeName = Regex.Escape(effectiveVar);
            var pattern = "^\\s*" + safeName + "\\s*=\\s*([\"']) (?<guid>[0-9a-fA-F\\-]{36}) \\1\\s*$";
            pattern = pattern.Replace(" ", string.Empty); // keep pattern readable above
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

            // Replace only the specific variable's GUID; keep other vars unchanged
            var updated = regex.Replace(text, m => m.Value.Replace(currentGuid, newGuid, StringComparison.OrdinalIgnoreCase), 1);
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

        // Try structured XML parsing first for reliable Type/SubType
        try
        {
            var xml = System.Xml.Linq.XDocument.Parse(content);
            var root = xml.Root;
            if (root != null)
            {
                string GetElem(string name)
                    => root.Descendants().FirstOrDefault(e => string.Equals(e.Name.LocalName, name, StringComparison.OrdinalIgnoreCase))?.Value?.Trim() ?? string.Empty;
                string GetChildElem(System.Xml.Linq.XElement parent, string name)
                    => parent.Elements().FirstOrDefault(e => string.Equals(e.Name.LocalName, name, StringComparison.OrdinalIgnoreCase))?.Value?.Trim() ?? string.Empty;

                var anyType = GetElem("Type").ToLowerInvariant();
                var subType = GetElem("SubType").ToLowerInvariant();
                var displayType = GetElem("displayType").ToLowerInvariant();

                // Paints
                if (anyType == "paints" || displayType.Contains("item_typepaints", StringComparison.OrdinalIgnoreCase))
                    return "paint";

                // Compute AttachDef details once
                var attachDef = root.Descendants().FirstOrDefault(e => string.Equals(e.Name.LocalName, "AttachDef", StringComparison.OrdinalIgnoreCase));
                var attachType = attachDef != null ? GetChildElem(attachDef, "Type").ToLowerInvariant() : string.Empty;
                var attachSubType = attachDef != null ? GetChildElem(attachDef, "SubType").ToLowerInvariant() : string.Empty;

                // Vehicles: detect early to avoid false positives from loadout content (which can include armor/weapons)
                bool hasVehicleComponent = root.Descendants().Any(e => string.Equals(e.Name.LocalName, "VehicleComponentParams", StringComparison.OrdinalIgnoreCase));
                bool isShip = hasVehicleComponent
                              || attachType.Contains("vehicle", StringComparison.OrdinalIgnoreCase)
                              || attachSubType.Contains("vehicle", StringComparison.OrdinalIgnoreCase)
                              || content.Contains("/spaceships/", StringComparison.OrdinalIgnoreCase)
                              || content.Contains("/ships/", StringComparison.OrdinalIgnoreCase)
                              || content.Contains("movementclass>spaceship<", StringComparison.OrdinalIgnoreCase);
                if (isShip)
                    return "ship";

                // Prefer explicit armor identification via AttachDef/SCItemSuitArmorParams/displayType
                bool hasArmorClass = attachType.StartsWith("char_armor_", StringComparison.OrdinalIgnoreCase)
                                     || displayType.Contains("armor", StringComparison.OrdinalIgnoreCase)
                                     || root.Descendants().Any(e => string.Equals(e.Name.LocalName, "SCItemSuitArmorParams", StringComparison.OrdinalIgnoreCase));
                if (hasArmorClass)
                {
                    var lower = content.ToLowerInvariant();
                    if (attachType.Contains("torso") || lower.Contains("torso") || lower.Contains("armor_core")) return "armorCore";
                    if (lower.Contains("helmet") || lower.Contains("_helmet")) return "armorHelmet";
                    if (lower.Contains("legs") || lower.Contains("_leg")) return "armorLegs";
                    if (lower.Contains("arms") || lower.Contains("forearm") || lower.Contains("gauntlet") || lower.Contains("glove")) return "armorArms";
                    // default to core for torso armor if ambiguous
                    return "armorCore";
                }

                // Weapons (only if not classified as armor)
                if (anyType == "weaponpersonal" || anyType == "weapon" || content.Contains("/weapons/", StringComparison.OrdinalIgnoreCase))
                {
                    if (subType == "knife" || displayType.Contains("knife", StringComparison.OrdinalIgnoreCase)) return "meele";
                    if (subType == "pistol" || displayType.Contains("pistol", StringComparison.OrdinalIgnoreCase) || content.Contains("pistol", StringComparison.OrdinalIgnoreCase)) return "sidearm";
                    return "fpsWeapon";
                }

                // Vehicles (fallback)
                if (anyType == "vehicle" || content.Contains("spaceship", StringComparison.OrdinalIgnoreCase) || content.Contains("/spaceships/", StringComparison.OrdinalIgnoreCase) || content.Contains("/ships/", StringComparison.OrdinalIgnoreCase))
                    return "ship";

                // Modules
                if (content.Contains("/modules/", StringComparison.OrdinalIgnoreCase) || displayType.Contains("module", StringComparison.OrdinalIgnoreCase))
                    return "moduleBase";
            }
        }
        catch
        {
            // fall back to heuristics below if not XML or malformed
        }

        // Heuristic fallback (keywords/paths), returns base names without family suffix
        var lowerText = content.ToLowerInvariant();
        bool HasWord(string w) => Regex.IsMatch(lowerText, @"\b" + Regex.Escape(w) + @"\b", RegexOptions.IgnoreCase);
        bool HasAny(params string[] words) => words.Any(HasWord);
        bool HasTokenLike(string token)
            => HasWord(token) || lowerText.Contains(token + "_") || lowerText.Contains("/" + token) || lowerText.Contains("\\" + token);

        if (lowerText.Contains("<type>paints</type>") || lowerText.Contains("/paints/") || lowerText.Contains("\\paints\\")
            || HasAny("paints", "paint", "livery") || lowerText.Contains("@item_typepaints"))
            return "paint";

        // Armor before weapons (records can list weapon ports)
        if (HasAny("armor", "armour") || lowerText.Contains("/armor/") || lowerText.Contains("\\armor\\")
            || lowerText.Contains("char_armor_") || lowerText.Contains("@item_displaytype_armor"))
        {
            if (HasTokenLike("helmet")) return "armorHelmet";
            if (HasTokenLike("core") || HasTokenLike("torso")) return "armorCore";
            if (HasTokenLike("legs") || HasTokenLike("leg")) return "armorLegs";
            if (HasTokenLike("arms") || lowerText.Contains("forearm") || lowerText.Contains("gauntlet") || lowerText.Contains("glove"))
                return "armorArms";
        }

        if (HasAny("weapon", "weapons") || lowerText.Contains("/weapons/") || lowerText.Contains("\\weapons\\")
            || lowerText.Contains("<type>weaponpersonal</type>"))
        {
            if (HasAny("melee", "meele", "knife", "blade", "sword", "axe", "dagger", "machete")
                || lowerText.Contains("<subtype>knife</subtype>")
                || lowerText.Contains("<tags>knife</tags>"))
                return "meele";

            if (HasAny("sidearm", "pistol")) return "sidearm";
            return "fpsWeapon";
        }

        if (HasAny("spaceship", "spaceships", "spacecraft", "ship", "vessel", "vehicle"))
            return "ship";

        if ((HasWord("module") || lowerText.Contains("/modules/", StringComparison.OrdinalIgnoreCase)) && HasWord("attach"))
            return "moduleBase";

        return null;
    }

    private static string? ResolveTargetVariableName(string scriptText, string baseVar, string mode)
    {
        // Enumerate existing variable names of the form: name = "GUID"
        var varRegex = new Regex("^\\s*([A-Za-z_][A-Za-z0-9_]*)\\s*=\\s*([\\\"'])[0-9a-fA-F\\-]{36}\\2\\s*$", RegexOptions.Multiline);
        var names = new HashSet<string>(varRegex.Matches(scriptText).Select(m => m.Groups[1].Value));

        string Prefer(params string[] candidates) => candidates.FirstOrDefault(names.Contains) ?? string.Empty;

        bool isOwned = string.Equals(mode, "owned", StringComparison.OrdinalIgnoreCase);
        int family = isOwned ? 1 : 2;

        // Normalize baseVar: strip trailing digits if any
        if (baseVar.EndsWith("1") || baseVar.EndsWith("2")) baseVar = baseVar[..^1];

        string pickWithFamily(string stem)
        {
            if (family == 1)
                return Prefer(stem + "1", stem) ;
            else
                return Prefer(stem + "2", stem + "1", stem);
        }

        switch (baseVar)
        {
            case "paint":
            case "sidearm":
            case "meele":
            case "fpsWeapon":
            case "armorHelmet":
            case "armorCore":
            case "armorLegs":
            case "armorArms":
            case "moduleBase":
                {
                    var stem = baseVar;
                    var chosen = pickWithFamily(stem);
                    return string.IsNullOrEmpty(chosen) ? null : chosen;
                }
            case "ship":
                {
                    // Strictly use ship1/ship2 only
                    var chosen = family == 1 ? Prefer("ship1") : Prefer("ship2");
                    return string.IsNullOrEmpty(chosen) ? null : chosen;
                }
            default:
                {
                    // If baseVar is already a concrete name (e.g., idris), try it directly and then family variants
                    var stem = baseVar;
                    var chosen = family == 1 ? Prefer(stem + "1", stem) : Prefer(stem + "2", stem + "1", stem);
                    return string.IsNullOrEmpty(chosen) ? null : chosen;
                }
        }
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
            content = ResolveXmlTags(content);
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