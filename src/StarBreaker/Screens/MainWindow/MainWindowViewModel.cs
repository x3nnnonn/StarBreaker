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
            App.Current.Services.GetRequiredService<HomeViewModel>(),
            App.Current.Services.GetRequiredService<AboutViewModel>(),
        ];
        
        CurrentPage = _pages.First();
    }
    
    [ObservableProperty]
    private IPageViewModel _currentPage;

    [ObservableProperty] 
    private ObservableCollection<IPageViewModel> _pages;
}