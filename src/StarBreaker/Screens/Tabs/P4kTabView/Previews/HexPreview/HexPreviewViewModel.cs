using AvaloniaHex.Document;
using CommunityToolkit.Mvvm.ComponentModel;

namespace StarBreaker.Screens;

public  sealed partial class HexPreviewViewModel : FilePreviewViewModel
{
    public HexPreviewViewModel(byte[] data)
    {
        Document = new MemoryBinaryDocument(data, true);
    }

    [ObservableProperty] private IBinaryDocument _document;
}