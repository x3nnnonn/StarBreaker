using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using StarBreaker.Models;
using StarBreaker.Services;

namespace StarBreaker.Screens;

public sealed partial class HomeViewModel : PageViewModelBase
{
    public override string Name => "Home";
    public override string Icon => "Home";

    private readonly IP4kService _p4KService;

    public HomeViewModel(IP4kService p4kService)
    {
        _p4KService = p4kService;
        Source = new HierarchicalTreeDataGridSource<ZipNode>(Array.Empty<ZipNode>())
        {
            Columns =
            {
                new CheckBoxColumn<ZipNode>(
                    null,
                    x => x.IsChecked,
                    (o, v) => o.IsChecked = v,
                    options: new CheckBoxColumnOptions<ZipNode>()
                    {
                        CanUserResizeColumn = false
                    }
                ),
                new HierarchicalExpanderColumn<ZipNode>(
                    new TextColumn<ZipNode, string>("File Name", x => x.Name, options: new TextColumnOptions<ZipNode>()
                    {
                        IsTextSearchEnabled = true
                    }),
                    x => x.Children.Values
                ),
                new TextColumn<ZipNode, string>("Size", x => x.SizeUi),
                new TextColumn<ZipNode, string>("Date", x => x.DateModifiedUi),
                // new TextColumn<ZipNode, string>("Compression", x => x.CompressionMethodUi),
                // new TextColumn<ZipNode, string>("Encrypted", x => x.EncryptedUi)
            },
        };

        Initialize();
    }

    [ObservableProperty] private HierarchicalTreeDataGridSource<ZipNode> _source;

    [ObservableProperty] private double? _progress;

    private void Initialize()
    {
        if (_p4KService.P4kFile == null)
            throw new InvalidOperationException("P4K file is not loaded");

        Progress = 0.0f;

        Task.Run(() =>
        {
            var progress = new Progress<double>(value => Dispatcher.UIThread.InvokeAsync(() => Progress = (float)value));
            var zipFileEntries = new ZipNode(_p4KService.P4kFile.Entries, progress);
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                Source.Items = zipFileEntries.Children.Values;
                Progress = null;
            });
        });
    }
}