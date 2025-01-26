using CommunityToolkit.Mvvm.ComponentModel;

namespace StarBreaker.Screens;

public sealed partial class TextPreviewViewModel : FilePreviewViewModel
{
    public TextPreviewViewModel(string text)
    {
        Text = text;
    }

    [ObservableProperty] private string _text;
}