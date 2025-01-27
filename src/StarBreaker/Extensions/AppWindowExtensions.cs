using Avalonia.Controls;
using Avalonia.Media;
using FluentAvalonia.UI.Windowing;

namespace StarBreaker.Extensions;

public static class AppWindowExtensions
{
    public static void EnableMicaTransparency(this AppWindow window)
    {
        window.TransparencyLevelHint = [WindowTransparencyLevel.Mica];
        window.Background = new SolidColorBrush(new Color(0, 0, 0, 0));
    }
}