using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace StarBreaker.Screens;

public partial class P4kTabView : UserControl
{
    private TreeDataGrid? _treeDataGrid;
    
    public P4kTabView()
    {
        InitializeComponent();
        
        _treeDataGrid = this.FindControl<TreeDataGrid>("TreeDataGrid");
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
    
    private void SearchBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is P4kTabViewModel viewModel)
        {
            viewModel.Search();
        }
    }
}