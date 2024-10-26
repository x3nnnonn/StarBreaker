using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StarBreaker.Screens;
using StarBreaker.Services;

namespace StarBreaker;

public static class DesignData
{
    public static SplashWindowViewModel SplashWindowViewModel { get; } = App.Current.Services.GetRequiredService<SplashWindowViewModel>();
    public static MainWindowViewModel MainWindowViewModel { get; } = App.Current.Services.GetRequiredService<MainWindowViewModel>();
    public static HomeViewModel HomeViewModel { get; } = App.Current.Services.GetRequiredService<HomeViewModel>();
}