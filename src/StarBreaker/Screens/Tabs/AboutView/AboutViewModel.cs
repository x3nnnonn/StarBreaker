using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using StarBreaker.Common;
using StarBreaker.Forge;
using StarBreaker.Services;

namespace StarBreaker.Screens;

public sealed partial class AboutViewModel : PageViewModelBase
{
    private const string dataCorePath = "Data\\Game.dcb";
    public override string Name => "About";
    public override string Icon => "Info";

    private readonly IP4kService _p4KService;

    public AboutViewModel(IP4kService p4kService)
    {
        _p4KService = p4kService;
        Forge = null;

        Task.Run(Initialize);
    }

    private void Initialize()
    {
        var entry = _p4KService.P4kFile.Entries.First(e => e.Name == dataCorePath);
        var stream = entry.Open();
        var forge = new DataForge(stream);
        stream.Dispose();
        
        Dispatcher.UIThread.InvokeAsync(() => Forge = forge);
    }

    [ObservableProperty] 
    private DataForge? _forge;

    public string Yes => Forge?.Database.RecordDefinitions.Length.ToString() ?? "No";
}