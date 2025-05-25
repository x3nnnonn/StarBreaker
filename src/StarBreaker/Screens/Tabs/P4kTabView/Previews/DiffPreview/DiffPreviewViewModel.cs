using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Linq;

namespace StarBreaker.Screens;

public sealed partial class DiffPreviewViewModel : FilePreviewViewModel
{
    [ObservableProperty] private string _oldContent = string.Empty;
    [ObservableProperty] private string _newContent = string.Empty;
    [ObservableProperty] private string _oldLabel = "Old Version";
    [ObservableProperty] private string _newLabel = "New Version";
    [ObservableProperty] private string _fileExtension = string.Empty;
    [ObservableProperty] private int _currentHunkIndex = -1;
    [ObservableProperty] private int _totalHunks = 0;
    [ObservableProperty] private string _changeInfo = "";
    [ObservableProperty] private bool _showFullFile = false;
    
    public DiffResult? DiffResult { get; private set; }
    public List<DiffHunk> Hunks => DiffResult?.Hunks ?? new();
    
    // Events for navigation
    public event System.Action<int>? NavigateToHunk;
    public event System.Action? ViewModeChanged;

    public DiffPreviewViewModel(string oldContent, string newContent, string oldLabel, string newLabel, string fileExtension)
    {
        OldContent = oldContent;
        NewContent = newContent;
        OldLabel = oldLabel;
        NewLabel = newLabel;
        FileExtension = fileExtension;
        
        // Compute diff for highlighting
        DiffResult = DiffAlgorithm.Compare(oldContent, newContent);
        
        // Setup hunk navigation
        if (DiffResult != null)
        {
            TotalHunks = DiffResult.Hunks.Count;
            UpdateChangeInfo();
        }
    }
    
    [RelayCommand]
    public void NextChange()
    {
        if (Hunks.Count == 0) return;
        
        CurrentHunkIndex = (CurrentHunkIndex + 1) % Hunks.Count;
        NavigateToHunk?.Invoke(CurrentHunkIndex);
        UpdateChangeInfo();
    }
    
    [RelayCommand]
    public void PreviousChange()
    {
        if (Hunks.Count == 0) return;
        
        CurrentHunkIndex = CurrentHunkIndex <= 0 ? Hunks.Count - 1 : CurrentHunkIndex - 1;
        NavigateToHunk?.Invoke(CurrentHunkIndex);
        UpdateChangeInfo();
    }
    
    [RelayCommand]
    public void FirstChange()
    {
        if (Hunks.Count == 0) return;
        
        CurrentHunkIndex = 0;
        NavigateToHunk?.Invoke(CurrentHunkIndex);
        UpdateChangeInfo();
    }
    
    [RelayCommand]
    public void ToggleFullFile()
    {
        ShowFullFile = !ShowFullFile;
        ViewModeChanged?.Invoke();
    }
    
    private void UpdateChangeInfo()
    {
        if (Hunks.Count == 0)
        {
            ChangeInfo = "No changes";
        }
        else if (CurrentHunkIndex >= 0)
        {
            ChangeInfo = $"Hunk {CurrentHunkIndex + 1} of {TotalHunks}";
        }
        else
        {
            ChangeInfo = $"{TotalHunks} hunks";
        }
    }
} 