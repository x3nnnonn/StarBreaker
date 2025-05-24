using Avalonia.Controls;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;

namespace StarBreaker.Screens;

public partial class TextPreviewView : UserControl
{
    private TextEditor? _textEditor;
    private TextMate.Installation? _textMateInstallation;
    private RegistryOptions? _registryOptions;
    
    public TextPreviewView()
    {
        InitializeComponent();
        _textEditor = this.FindControl<TextEditor>("TextEditor");
        
        // Initialize TextMate for syntax highlighting
        if (_textEditor != null)
        {
            _registryOptions = new RegistryOptions(ThemeName.DarkPlus);
            _textMateInstallation = _textEditor.InstallTextMate(_registryOptions);
        }
        
        // Subscribe to DataContext changes
        DataContextChanged += OnDataContextChanged;
    }
    
    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_textEditor != null && DataContext is TextPreviewViewModel vm)
        {
            _textEditor.Text = vm.Text ?? string.Empty;
            
            // Apply syntax highlighting based on file extension and content
            if (_textMateInstallation != null && _registryOptions != null)
            {
                var language = DetectLanguageFromExtension(vm.FileExtension) ?? DetectLanguageFromContent(vm.Text);
                if (!string.IsNullOrEmpty(language))
                {
                    try
                    {
                        var scope = _registryOptions.GetScopeByLanguageId(language);
                        if (scope != null)
                        {
                            _textMateInstallation.SetGrammar(scope);
                        }
                    }
                    catch
                    {
                        // If the specific language isn't available, clear highlighting
                        _textMateInstallation.SetGrammar(null);
                    }
                }
                else
                {
                    // Clear syntax highlighting for unknown content
                    _textMateInstallation.SetGrammar(null);
                }
            }
        }
    }
    
    private static string? DetectLanguageFromExtension(string? fileExtension)
    {
        if (string.IsNullOrEmpty(fileExtension))
            return null;
            
        return fileExtension.ToLowerInvariant() switch
        {
            ".xml" => "xml",
            ".json" => "json",
            ".cfg" => "ini", // Configuration files often use INI-like syntax
            ".ini" => "ini", // INI files (including Star Citizen localization files)
            ".txt" => "plaintext",
            ".mtl" => "xml", // Material files in Star Citizen are often XML-based
            ".eco" => "xml", // Eco files are typically XML
            ".lua" => "lua",
            ".js" => "javascript",
            ".hlsl" => "hlsl",
            ".fx" => "hlsl", // Effect files
            ".shader" => "hlsl",
            ".cryxml" => "xml",
            _ => null
        };
    }
    
    private static bool IsXmlContent(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return false;
            
        var trimmed = text.TrimStart();
        return trimmed.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("<", StringComparison.OrdinalIgnoreCase);
    }
    
    private static string? DetectLanguageFromContent(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return null;
            
        var trimmed = text.TrimStart();
        
        // XML detection
        if (IsXmlContent(text))
            return "xml";
        
        // JSON detection
        if (trimmed.StartsWith("{") || trimmed.StartsWith("["))
            return "json";
            
        // Check for common file type indicators in the content
        if (trimmed.Contains("shader") || trimmed.Contains("technique") || trimmed.Contains("Technique"))
            return "hlsl"; // For shader/material files
            
        return null; // No specific language detected
    }
}