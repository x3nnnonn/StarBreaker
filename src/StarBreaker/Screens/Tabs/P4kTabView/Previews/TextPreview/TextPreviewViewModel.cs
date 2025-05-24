using CommunityToolkit.Mvvm.ComponentModel;

namespace StarBreaker.Screens;

public sealed partial class TextPreviewViewModel : FilePreviewViewModel
{
    public TextPreviewViewModel(string text, string? fileExtension = null)
    {
        Text = text;
        FileExtension = fileExtension;
    }

    [ObservableProperty] private string _text;
    [ObservableProperty] private string? _fileExtension;
}