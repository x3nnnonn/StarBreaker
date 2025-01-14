using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using Pfim;
using SkiaSharp;
using StarBreaker.Extensions;
using AvaloniaIImage = Avalonia.Media.IImage;

namespace StarBreaker.Screens;

public partial class DdsPreviewViewModel : FilePreviewViewModel
{
    public DdsPreviewViewModel(Avalonia.Media.Imaging.Bitmap bitmap)
    {
        Image = bitmap;
    }

    [ObservableProperty] private AvaloniaIImage? _image;
}