using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StarBreaker.Screens;
using StarBreaker.Services;

namespace StarBreaker.Extensions;

public static class ServiceCollectionExtensions
{
    public static void RegisterServices(this ServiceCollection services)
    {
        services.AddLogging(b => { b.AddSimpleConsole(options => { options.SingleLine = true; }); });
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<SplashWindowViewModel>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<AboutViewModel>();
        services.AddSingleton<IP4kService, P4kService>();
    }
}