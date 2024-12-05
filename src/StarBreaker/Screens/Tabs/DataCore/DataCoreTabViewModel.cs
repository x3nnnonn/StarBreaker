using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using StarBreaker.DataCore;
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
        DataCore = null;

        Task.Run(Initialize);
    }

    private void Initialize()
    {
        var entry = _p4KService.P4kFile.OpenRead(dataCorePath);
        var dcb = new DataCoreBinary(entry);
        entry.Dispose();
        
        Dispatcher.UIThread.InvokeAsync(() => DataCore = dcb);
    }

    [ObservableProperty] 
    private DataCoreBinary? _dataCore;

    public string Yes => DataCore?.Database.RecordDefinitions.Length.ToString() ?? "No";
}