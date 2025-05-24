using System.Collections.ObjectModel;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StarBreaker.Services;

namespace StarBreaker.Screens;

public partial class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel()
    {
        Pages =
        [
            App.Current.Services.GetRequiredService<P4kTabViewModel>(),
            App.Current.Services.GetRequiredService<DataCoreTabViewModel>(),
            App.Current.Services.GetRequiredService<DiffTabViewModel>(),
        ];
        
        CurrentPage = _pages.First();
    }
    
    [ObservableProperty]
    private PageViewModelBase _currentPage;

    [ObservableProperty] 
    private ObservableCollection<PageViewModelBase> _pages;
}