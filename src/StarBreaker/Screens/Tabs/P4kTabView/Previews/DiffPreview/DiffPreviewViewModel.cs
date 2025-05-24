using CommunityToolkit.Mvvm.ComponentModel;

namespace StarBreaker.Screens;

public sealed partial class DiffPreviewViewModel : FilePreviewViewModel
{
    [ObservableProperty] private string _oldContent = string.Empty;
    [ObservableProperty] private string _newContent = string.Empty;
    [ObservableProperty] private string _oldLabel = "Old Version";
    [ObservableProperty] private string _newLabel = "New Version";
    [ObservableProperty] private string _fileExtension = string.Empty;

    public DiffPreviewViewModel(string oldContent, string newContent, string oldLabel, string newLabel, string fileExtension)
    {
        OldContent = oldContent;
        NewContent = newContent;
        OldLabel = oldLabel;
        NewLabel = newLabel;
        FileExtension = fileExtension;
    }
} 