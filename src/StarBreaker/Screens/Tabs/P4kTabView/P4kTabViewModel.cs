using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Selection;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using StarBreaker.Extensions;
using StarBreaker.P4k;
using StarBreaker.Services;

namespace StarBreaker.Screens;

public sealed partial class P4kTabViewModel : PageViewModelBase
{
    public override string Name => "P4k";
    public override string Icon => "ZipFolder";

    private readonly ILogger<P4kTabViewModel> _logger;
    private readonly IP4kService _p4KService;
    private readonly IPreviewService _previewService;

    [ObservableProperty] private HierarchicalTreeDataGridSource<IP4kNode> _source;

    [ObservableProperty] private FilePreviewViewModel? _preview;


    public P4kTabViewModel(IP4kService p4kService, ILogger<P4kTabViewModel> logger, IPreviewService previewService)
    {
        _p4KService = p4kService;
        _logger = logger;
        _previewService = previewService;
        Source = new HierarchicalTreeDataGridSource<IP4kNode>(Array.Empty<IP4kNode>())
        {
            Columns =
            {
                new HierarchicalExpanderColumn<IP4kNode>(
                    new TextColumn<IP4kNode, string>("Name", x => x.GetName(), options: new TextColumnOptions<IP4kNode>()
                    {
                        //TODO: make the sorting do folders first, then files
                        //CompareAscending = null,
                        //CompareDescending = null
                    }),
                    x => x.GetChildren()
                ),
                new TextColumn<IP4kNode, string>("Size", x => x.GetSize(), options: new TextColumnOptions<IP4kNode>()
                {
                    CompareAscending = (a, b) => (a?.SizeOrZero() ?? 0).CompareTo(b?.SizeOrZero() ?? 0),
                    CompareDescending = (a, b) => (b?.SizeOrZero() ?? 0).CompareTo(a?.SizeOrZero() ?? 0)
                }),
                new TextColumn<IP4kNode, string>("Date", x => x.GetDate()),
            },
        };

        Source.RowSelection!.SingleSelect = true;
        Source.RowSelection.SelectionChanged += SelectionChanged;
        Source.Items = _p4KService.P4KFileSystem.Root.Children.Values;
    }

    private void SelectionChanged(object? sender, TreeSelectionModelSelectionChangedEventArgs<IP4kNode> e)
    {
        if (e.SelectedItems.Count != 1)
            return;

        //TODO: here, set some boolean that locks the entire UI until the preview is loaded.
        //That is needed to prevent race conditions where the user clicks on another file before the preview is loaded.

        //TODO: tabs?

        var selectedEntry = e.SelectedItems[0];
        if (selectedEntry == null)
        {
            Preview = null;
            return;
        }

        if (selectedEntry is not P4kFileNode selectedFile)
        {
            //we clicked on a folder, do nothing to the preview.
            return;
        }

        if (selectedFile.ZipEntry.UncompressedSize > int.MaxValue)
        {
            _logger.LogWarning("File too big to preview");
            return;
        }

        //todo: for a big ass file show a loading screen or something
        Preview = null;
        Task.Run(() =>
        {
            try
            {
                var p = _previewService.GetPreview(selectedFile);
                Dispatcher.UIThread.Post(() => Preview = p);
            }
            // catch (Exception exception)
            // {
            //     _logger.LogError(exception, "Failed to preview file");
            // }
            finally { }
        });
    }
}