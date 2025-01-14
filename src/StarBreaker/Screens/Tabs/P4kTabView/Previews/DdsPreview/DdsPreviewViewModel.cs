using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace StarBreaker.Screens;

public partial class DdsPreviewViewModel : FilePreviewViewModel
{
    public DdsPreviewViewModel(IImage image)
    {
        Image = image;
    }
//Data\Textures\physical_based_global\unified_detail\brushed_detail.dds
    [ObservableProperty] private IImage? _image;
}