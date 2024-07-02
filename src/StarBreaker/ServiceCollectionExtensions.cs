using Microsoft.Extensions.DependencyInjection;
using StarBreaker.Screens;
using StarBreaker.Services;

namespace StarBreaker;

public static class ServiceCollectionExtensions {
    public static void AddServices(this IServiceCollection collection) {
        collection.AddSingleton<IP4kService, P4KService>();
        collection.AddTransient<MainWindowViewModel>();
    }
}