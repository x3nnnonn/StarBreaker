using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaloniaEdit;
using AvaloniaEdit.Highlighting;
using System;
using System.IO;

namespace StarBreaker.Screens;

public partial class DiffPreviewView : UserControl
{
    private TextEditor? _oldEditor;
    private TextEditor? _newEditor;

    public DiffPreviewView()
    {
        InitializeComponent();
        InitializeEditors();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void InitializeEditors()
    {
        _oldEditor = this.FindControl<TextEditor>("OldEditor");
        _newEditor = this.FindControl<TextEditor>("NewEditor");
        
        // Note: Scroll synchronization removed for now due to API issues
        // Can be added back later when we find the correct way to access scroll events
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is DiffPreviewViewModel viewModel)
        {
            SetupEditorContent(viewModel);
        }
    }

    private void SetupEditorContent(DiffPreviewViewModel viewModel)
    {
        if (_oldEditor == null || _newEditor == null) return;

        // Set content
        _oldEditor.Text = viewModel.OldContent;
        _newEditor.Text = viewModel.NewContent;

        // Set syntax highlighting based on file extension
        var highlighting = GetSyntaxHighlighting(viewModel.FileExtension);
        if (highlighting != null)
        {
            _oldEditor.SyntaxHighlighting = highlighting;
            _newEditor.SyntaxHighlighting = highlighting;
        }
    }

    private IHighlightingDefinition? GetSyntaxHighlighting(string fileExtension)
    {
        try
        {
            var highlightingManager = HighlightingManager.Instance;
            
            return fileExtension.ToLowerInvariant() switch
            {
                ".xml" or ".dcb" => highlightingManager.GetDefinition("XML"),
                ".json" => highlightingManager.GetDefinition("JavaScript"), // Close enough for JSON
                ".lua" => highlightingManager.GetDefinition("JavaScript"), // Close enough for Lua
                ".cfg" or ".ini" => highlightingManager.GetDefinition("INI"),
                ".py" => highlightingManager.GetDefinition("Python"),
                ".cpp" or ".c" or ".h" => highlightingManager.GetDefinition("C++"),
                ".cs" => highlightingManager.GetDefinition("C#"),
                ".js" => highlightingManager.GetDefinition("JavaScript"),
                ".html" or ".htm" => highlightingManager.GetDefinition("HTML"),
                ".css" => highlightingManager.GetDefinition("CSS"),
                ".sql" => highlightingManager.GetDefinition("SQL"),
                ".php" => highlightingManager.GetDefinition("PHP"),
                ".txt" or "" => null, // Plain text
                _ => null
            };
        }
        catch
        {
            return null; // Fallback to plain text
        }
    }
} 