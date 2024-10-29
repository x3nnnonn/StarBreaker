using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using StarBreaker.Common;
using StarBreaker.Forge;
using StarBreaker.Services;

namespace StarBreaker.Screens;

public sealed partial class DataCoreTabViewModel : PageViewModelBase
{
    private const string dataCorePath = "Data\\Game.dcb";
    public override string Name => "DataCore";
    public override string Icon => "ViewAll";

    private readonly IP4kService _p4KService;

    public DataCoreTabViewModel(IP4kService p4kService)
    {
        _p4KService = p4kService;
        Forge = null;

        Task.Run(Initialize);
    }

    private void Initialize()
    {
        var entry = _p4KService.P4kFile.Entries[dataCorePath];
        var stream = _p4KService.P4kFile.Open(entry);
        var forge = new DataForge(stream);
        stream.Dispose();
        
        Dispatcher.UIThread.InvokeAsync(() => Forge = forge);
    }

    [ObservableProperty] 
    private DataForge? _forge;

    public string Yes => Forge?.Database.RecordDefinitions.Length.ToString() ?? "No";
}