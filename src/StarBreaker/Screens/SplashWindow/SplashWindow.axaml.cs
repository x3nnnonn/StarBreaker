using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using FluentAvalonia.UI.Windowing;
using StarBreaker.Extensions;

namespace StarBreaker.Screens;

public partial class SplashWindow : AppWindow
{
    public SplashWindow()
    {
        InitializeComponent();
        //this.EnableMicaTransparency();
    }
}