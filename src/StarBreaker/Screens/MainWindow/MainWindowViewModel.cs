using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Threading;
using ReactiveUI;
using StarBreaker.Models;
using StarBreaker.P4k;
using StarBreaker.Services;

namespace StarBreaker.Screens;

public class MainWindowViewModel : ViewModelBase
{
private readonly IP4kService _p4KService;

    public MainWindowViewModel(IP4kService p4KService)
    {
        _p4KService = p4KService;
        LoadingMessage = "Loading...";
        IsLoading = true;
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
                new TextColumn<ZipNode, string>("Compression Method", x => x.CompressionMethodUi),
                new TextColumn<ZipNode, string>("Encrypted", x => x.EncryptedUi)
            },
        };
        
        Task.Run(Initialize);
    }

    private string _loadingMessage = "";

    public string LoadingMessage
    {
        get => _loadingMessage;
        set => this.RaiseAndSetIfChanged(ref _loadingMessage, value);
    }

    private bool _isLoading;

    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    public HierarchicalTreeDataGridSource<ZipNode> Source { get; }

    private void Initialize()
    {
        if (_p4KService.P4k == null)
        {
            Dispatcher.UIThread.InvokeAsync(() => LoadingMessage = "Loading p4k...");
            _p4KService.LoadP4k(@"C:\Program Files\Roberts Space Industries\StarCitizen\LIVE\Data.p4k");
        }

        Dispatcher.UIThread.InvokeAsync(() => LoadingMessage = "Processing entries...");
        var zipFileEntries = GetZipEntries(_p4KService.P4k!.Entries);

        Dispatcher.UIThread.InvokeAsync(() =>
        {
            Source.Items = zipFileEntries;
            IsLoading = false;
        });
    }

    public static IEnumerable<ZipNode> GetZipEntries(IEnumerable<ZipEntry> zipEntries)
    {
        var root = new ZipNode(zipEntries);
        
        return root.Children.Values;
    }
}