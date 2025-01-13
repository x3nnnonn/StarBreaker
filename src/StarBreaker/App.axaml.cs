using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StarBreaker.Extensions;
using StarBreaker.Screens;
using StarBreaker.Services;

namespace StarBreaker;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        if (Design.IsDesignMode)
        {
            RequestedThemeVariant = ThemeVariant.Dark;
        }
    }
    
    private SplashWindow? _splashWindow;
    private MainWindow? _mainWindow;

    public override void OnFrameworkInitializationCompleted()
    {
        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
#pragma warning disable IL2026
        BindingPlugins.DataValidators.RemoveAt(0);
#pragma warning restore IL2026

        var collection = new ServiceCollection();

        collection.RegisterServices(Design.IsDesignMode);
        ViewLocator.RegisterViews();

        Services = collection.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var splashVm = Services.GetRequiredService<SplashWindowViewModel>();
            _splashWindow = new SplashWindow { DataContext = splashVm };

            splashVm.P4kLoaded += SwapWindows;

            desktop.MainWindow = _splashWindow;
            _splashWindow.Show();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void SwapWindows(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) 
            return;
        
        var mainVm = Services.GetRequiredService<MainWindowViewModel>();
        _mainWindow = new MainWindow { DataContext = mainVm };
                
        //do not change the order of these
        _mainWindow.Show();
        _splashWindow!.Close();

        desktop.MainWindow = _mainWindow;
    }

    public new static App Current => Application.Current as App ?? throw new InvalidOperationException("App.Current is null");

    public static IStorageProvider StorageProvider => (Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow?.StorageProvider ??
                                                      throw new InvalidOperationException("StorageProvider is null");

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> instance to resolve application services.
    /// </summary>
    public IServiceProvider Services { get; private set; } = null!;
}