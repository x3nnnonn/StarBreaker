using Avalonia.Controls;
using Avalonia.Controls.Templates;
using StarBreaker.Screens;

namespace StarBreaker;

public class ViewLocator : IDataTemplate
{
    public static void RegisterViews()
    {
        var viewModelType = typeof(ViewModelBase);
        var controlType = typeof(Control);

        var viewModelTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => viewModelType.IsAssignableFrom(t) && !t.IsAbstract);

        foreach (var viewModel in viewModelTypes)
        {
            var viewName = viewModel.FullName?.Replace("ViewModel", "View");
            var view = viewName != null ? Type.GetType(viewName) : null;

            if (view != null && controlType.IsAssignableFrom(view))
            {
                var instance = Activator.CreateInstance(view);
                if (instance is not Control control)
                    throw new InvalidOperationException($"View {viewName} does not inherit from Control.");

                Registration.Add(viewModel, () => control);
            }
        }
    }

    private static readonly Dictionary<Type, Func<Control>> Registration = new();

    public Control Build(object? data)
    {
        var type = data?.GetType();
        if (type == null)
            return new TextBlock { Text = "Null" };

        if (Registration.TryGetValue(type, out var factory))
            return factory();

        return new TextBlock { Text = "Not Found: " + type };
    }

    public bool Match(object? data) => data is ViewModelBase;
}