using System.Collections.ObjectModel;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using StarBreaker.Services;

namespace StarBreaker.Screens;

public sealed partial class SplashWindowViewModel : ViewModelBase
{
    private readonly ILogger<SplashWindowViewModel> _logger;
    private readonly IP4kService _p4kService;

    [ObservableProperty] private ObservableCollection<StarCitizenInstallationViewModel> _installations;
    [ObservableProperty] private double? _progress;

    public SplashWindowViewModel(ILogger<SplashWindowViewModel> logger, IP4kService p4kService)
    {
        _logger = logger;
        _p4kService = p4kService;
        _installations = [];
        Progress = null;
        LoadDefaultP4kLocations();
    }

    public event EventHandler? P4kLoaded;

    private void OnP4kLoaded()
    {
        P4kLoaded?.Invoke(this, EventArgs.Empty);
    }

    private async Task LoadP4k(string path)
    {
        Progress = 0;
        var progress = new Progress<double>(x => Dispatcher.UIThread.Post(() => Progress = x));

        await Task.Run(() => _p4kService.OpenP4k(path, progress));

        OnP4kLoaded();
    }

    [RelayCommand]
    public async Task PickP4k()
    {
        _logger?.LogTrace("PickP4k enter");
        var defaultPath = await App.StorageProvider.TryGetFolderFromPathAsync(Constants.DefaultStarCitizenFolder);
        var task = App.StorageProvider.OpenFilePickerAsync(Constants.GetP4kFilter(defaultPath));
        var file = await task;
        if (file.Count != 1)
        {
            _logger?.LogError("OpenFilePickerAsync returned {Count} files", file.Count);
            return;
        }

        _logger?.LogTrace("PickP4k exit: {Path}", file[0].Path);

        await LoadP4k(file[0].Path.LocalPath);
    }

    public void LoadDefaultP4kLocations()
    {
        var p4ks = Directory.GetFiles(Constants.DefaultStarCitizenFolder, Constants.DataP4k, SearchOption.AllDirectories);
        if (p4ks.Length == 0)
        {
            _logger.LogError("No Data.p4k files found");
            return;
        }

        _logger.LogTrace("Found {Count} Data.p4k files", p4ks.Length);

        foreach (var p4k in p4ks)
        {
            var directoryName = Path.GetDirectoryName(p4k);
            if (directoryName is null)
            {
                _logger.LogError("Failed to get directory name for {Path}", p4k);
                continue;
            }

            Installations.Add(new StarCitizenInstallationViewModel
            {
                ChannelName = new DirectoryInfo(directoryName).Name,
                Path = p4k
            });
        }
    }

    [RelayCommand]
    public async Task ClickP4kLocation(string file)
    {
        _logger.LogTrace("ClickP4kLocation {Path}", file);
        await LoadP4k(file);
    }
}

public sealed class StarCitizenInstallationViewModel
{
    public required string ChannelName { get; init; }
    public required string Path { get; init; }
}