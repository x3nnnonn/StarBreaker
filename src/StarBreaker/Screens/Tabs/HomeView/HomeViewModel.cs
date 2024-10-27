using System.Buffers;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Selection;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using StarBreaker.Common;
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
                new HierarchicalExpanderColumn<ZipNode>(
                    new TextColumn<ZipNode, string>("File Name", x => x.Name, options: new TextColumnOptions<ZipNode>()),
                    x => x.Children.Values
                ),
                new TextColumn<ZipNode, string>("Size", x => x.SizeUi),
                new TextColumn<ZipNode, string>("Date", x => x.DateModifiedUi),
                // new TextColumn<ZipNode, string>("Compression", x => x.CompressionMethodUi),
                // new TextColumn<ZipNode, string>("Encrypted", x => x.EncryptedUi)
            },
        };

        Source.RowSelection!.SingleSelect = true;
        Source.RowSelection.SelectionChanged += SelectionChanged;
        Initialize();
    }

    private void SelectionChanged(object? sender, TreeSelectionModelSelectionChangedEventArgs<ZipNode> e)
    {
        if (e.SelectedItems.Count != 1)
            return;

        //TODO: here, set some boolean that locks the entire UI until the preview is loaded.
        //That is needed to prevent race conditions where the user clicks on another file before the preview is loaded.
        
        var selectedEntry = e.SelectedItems[0];
        if (selectedEntry == null)
        {
            Preview = null;
            return;
        }

        if (selectedEntry.ZipEntry == null)
        {
            //we clicked on a folder, do nothing to the preview.
            Console.WriteLine(selectedEntry.Name);
            return;
        }

        //if above 1gb, don't load
        if (selectedEntry.ZipEntry.UncompressedSize > 1024 * 1024 * 1024)
            return;

        //todo: for a big ass file show a loading screen or something
        Preview = null;
        Task.Run(() =>
        {
            var stream = selectedEntry.ZipEntry.Open();
            var buffer = new byte[(int)selectedEntry.ZipEntry.UncompressedSize];
            stream.ReadToEnd(buffer);
            stream.Dispose();

            FilePreviewViewModel preview;

            //check cryxml before extension since ".xml" sometimes is cxml sometimes plaintext
            if (CryXmlB.CryXml.TryOpen(buffer, out var c))
            {
                var stringwriter = new StringWriter();
                c.WriteXml(stringwriter);
                preview = new TextPreviewViewModel(stringwriter.ToString());
            }
            else if (plaintextExtensions.Any(p => selectedEntry.Name.EndsWith(p, StringComparison.InvariantCultureIgnoreCase)))
            {
                preview = new TextPreviewViewModel(Encoding.UTF8.GetString(buffer));
            }
            else if (ddsLodExtensions.Any(p => selectedEntry.Name.EndsWith(p, StringComparison.InvariantCultureIgnoreCase)))
            {
                preview = new DdsPreviewViewModel(buffer);
            }
            else
            {
                preview = new HexPreviewViewModel(buffer);
            }
            //todo other types

            Dispatcher.UIThread.Post(() => Preview = preview);
        });
    }

    [ObservableProperty] private HierarchicalTreeDataGridSource<ZipNode> _source;

    [ObservableProperty] private double? _progress;

    [ObservableProperty] private FilePreviewViewModel? _preview;

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

    private static readonly string[] plaintextExtensions = [".cfg", ".xml", ".txt", ".json"];

    private static readonly string[] ddsLodExtensions = [".dds"];
    //, ".dds.1", ".dds.2", ".dds.3", ".dds.4", ".dds.5", ".dds.6", ".dds.7", ".dds.8", ".dds.9"];
}