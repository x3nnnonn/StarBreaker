using Avalonia.Controls;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;
using System.ComponentModel;

namespace StarBreaker.Screens;

public partial class DataCoreTabView : UserControl
{
    private TextEditor? _xmlEditor;
    private TextMate.Installation? _textMateInstallation;
    private RegistryOptions? _registryOptions;
    
    public DataCoreTabView()
    {
        InitializeComponent();
        _xmlEditor = this.FindControl<TextEditor>("XmlEditor");
        
        // Initialize TextMate for XML syntax highlighting
        if (_xmlEditor != null)
        {
            _registryOptions = new RegistryOptions(ThemeName.DarkPlus);
            _textMateInstallation = _xmlEditor.InstallTextMate(_registryOptions);
            
            // Set XML grammar for DataCore content (which is always XML)
            try
            {
                var xmlScope = _registryOptions.GetScopeByLanguageId("xml");
                if (xmlScope != null)
                {
                    _textMateInstallation.SetGrammar(xmlScope);
                }
            }
            catch
            {
                // If XML grammar isn't available, continue without highlighting
            }
        }
        
        // Subscribe to DataContext changes
        DataContextChanged += OnDataContextChanged;
    }
    
    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        // Unsubscribe from previous view model
        if (sender is DataCoreTabView control && control.Tag is INotifyPropertyChanged oldVm)
        {
            oldVm.PropertyChanged -= OnViewModelPropertyChanged;
        }
        
        // Subscribe to new view model
        if (DataContext is INotifyPropertyChanged newVm)
        {
            newVm.PropertyChanged += OnViewModelPropertyChanged;
            Tag = newVm; // Store reference for cleanup
            
            // Update initial content
            UpdateXmlContent();
        }
    }
    
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DataCoreTabViewModel.SelectedRecordContent))
        {
            UpdateXmlContent();
        }
    }
    
    private void UpdateXmlContent()
    {
        if (_xmlEditor != null && DataContext is DataCoreTabViewModel vm)
        {
            _xmlEditor.Text = vm.SelectedRecordContent ?? string.Empty;
        }
    }
}