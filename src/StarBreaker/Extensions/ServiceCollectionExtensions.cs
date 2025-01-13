using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StarBreaker.Screens;
using StarBreaker.Services;

namespace StarBreaker.Extensions;

public static class ServiceCollectionExtensions
{
    public static void RegisterServices(this ServiceCollection services, bool isDesignMode)
    {
        services.AddLogging(b =>
        {
            b.AddDebug();
            b.AddConsole();
        });
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<SplashWindowViewModel>();
        services.AddTransient<P4kTabViewModel>();
        services.AddTransient<DataCoreTabViewModel>();

        if (isDesignMode)
        {
            services.AddSingleton<IP4kService, DesignP4kService>();
        }
        else
        {
            services.AddSingleton<IP4kService, P4kService>();
        }
    }
}