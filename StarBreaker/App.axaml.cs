using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using StarBreaker.Screens;
using StarBreaker.Services;

namespace StarBreaker;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            throw new InvalidOperationException("This template only supports Desktop application style.");

        // If you use CommunityToolkit, line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0);

        // Register all the services needed for the application to run
        var collection = new ServiceCollection();
        collection.AddServices();

        // Creates a ServiceProvider containing services from the provided IServiceCollection
        var services = collection.BuildServiceProvider();
        
        if (desktop.Args?.Length == 1 && desktop.Args[0].EndsWith(".p4k", StringComparison.OrdinalIgnoreCase))
        {
            // Load the p4k file if it was passed as an argument. Otherwise, the user will be prompted.
            var p4kService = services.GetRequiredService<IP4kService>();
            p4kService.LoadP4k(desktop.Args[0]);
        }
        
        var vm = services.GetRequiredService<MainWindowViewModel>();
        desktop.MainWindow = new MainWindow
        {
            DataContext = vm
        };

        base.OnFrameworkInitializationCompleted();
    }
}