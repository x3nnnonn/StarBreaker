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

    public DiffTabViewModel(ILogger<DiffTabViewModel> logger)
    {
        _logger = logger;
        LoadSettings();
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
                    if (folderName == "LIVE" || folderName == "PTU" || folderName == "EPTU")
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
                     Directory.Exists(Path.Combine(GameFolder, "EPTU")))
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
        
        var basePath = GetBaseInstallationPath();
        var channelPath = Path.Combine(basePath, channel);
        
        if (Directory.Exists(channelPath))
        {
            GameFolder = channelPath;
            AddLogMessage($"Selected {channel} channel: {channelPath}");
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
                var outputDir = Path.Combine(OutputDirectory, "P4k");
                
                // Count total nodes for progress tracking
                var totalNodes = CountNodes(p4k.Root);
                var processedNodes = 0;
                var lastReportedProgress = 0.0;
                
                WriteFileForNode(outputDir, p4k.Root, () =>
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
                progressCallback(0.3);
                
                var outputDir = Path.Combine(OutputDirectory, "Protobuf");
                Directory.CreateDirectory(outputDir);
                
                progressCallback(0.7);
                
                // Note: Protobuf extraction is complex and would require additional CLI integration
                // For now, we'll just create the directory and log that it would be done
                AddLogMessage("Protobuf extraction placeholder - would extract protobuf definitions from executable.");
                
                progressCallback(1.0);
            });
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
                        new System.Xml.Linq.XAttribute("Name", Path.GetFileName(childFileNode.ZipEntry.Name)),
                        new System.Xml.Linq.XAttribute("CRC32", $"0x{childFileNode.ZipEntry.Crc32:X8}"),
                        new System.Xml.Linq.XAttribute("Size", childFileNode.ZipEntry.UncompressedSize.ToString()),
                        new System.Xml.Linq.XAttribute("CompressionType", childFileNode.ZipEntry.CompressionMethod.ToString()),
                        new System.Xml.Linq.XAttribute("Encrypted", childFileNode.ZipEntry.IsCrypted.ToString())
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
} 