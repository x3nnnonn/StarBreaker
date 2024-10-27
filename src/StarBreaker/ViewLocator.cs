using Avalonia.Controls;
using Avalonia.Controls.Templates;
using StarBreaker.Screens;

namespace StarBreaker;

public class ViewLocator : IDataTemplate
{
    public static void RegisterViews()
    {
        Register<MainWindowViewModel, MainWindow>();
        Register<SplashWindowViewModel, SplashWindow>();
        Register<HomeViewModel, HomeView>();
        Register<AboutViewModel, AboutView>();
        Register<HexPreviewViewModel, HexPreviewView>();
        Register<TextPreviewViewModel, TextPreviewView>();
        Register<DdsPreviewViewModel, DdsPreviewView>();
    }
    
    private static readonly Dictionary<Type, Func<Control>> Registration = new();

    public static void Register<TViewModel, TView>() where TView : Control, new() where TViewModel : ViewModelBase
    {
        Registration.Add(typeof(TViewModel), () => new TView());
    }

    public Control Build(object? data)
    {
        var type = data?.GetType();
        if (type == null)
        {
            return new TextBlock { Text = "Null" };
        }

        if (Registration.TryGetValue(type, out var factory))
        {
            return factory();
        }
        else
        {
            return new TextBlock { Text = "Not Found: " + type };
        }
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}