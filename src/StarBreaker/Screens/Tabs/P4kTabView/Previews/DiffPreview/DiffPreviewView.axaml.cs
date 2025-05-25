using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Highlighting;
using System;
using System.Linq;

namespace StarBreaker.Screens;

public partial class DiffPreviewView : UserControl
{
    private ScrollViewer? _diffScrollViewer;
    private StackPanel? _diffContent;
    private DiffPreviewViewModel? _currentViewModel;

    public DiffPreviewView()
    {
        InitializeComponent();
        InitializeControls();
        
        // Enable keyboard handling
        Focusable = true;
        KeyDown += OnKeyDown;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void InitializeControls()
    {
        _diffScrollViewer = this.FindControl<ScrollViewer>("DiffScrollViewer");
        _diffContent = this.FindControl<StackPanel>("DiffContent");
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        // Unsubscribe from old view model  
        if (_currentViewModel != null)
        {
            _currentViewModel.NavigateToHunk -= OnNavigateToHunk;
            _currentViewModel.ViewModeChanged -= OnViewModeChanged;
        }

        if (DataContext is DiffPreviewViewModel viewModel)
        {
            _currentViewModel = viewModel;
            SetupDiffContent(viewModel);
            
            // Subscribe to navigation events
            viewModel.NavigateToHunk += OnNavigateToHunk;
            viewModel.ViewModeChanged += OnViewModeChanged;
            
            // Auto-navigate to first hunk if available
            if (viewModel.Hunks.Count > 0)
            {
                viewModel.FirstChangeCommand.Execute(null);
            }
        }
        else
        {
            _currentViewModel = null;
        }
    }

    private void SetupDiffContent(DiffPreviewViewModel viewModel)
    {
        if (_diffContent == null) return;

        _diffContent.Children.Clear();

        if (viewModel.ShowFullFile)
        {
            CreateFullFileView(viewModel);
        }
        else
        {
            CreateUnifiedDiffView(viewModel);
        }
    }

    private void CreateUnifiedDiffView(DiffPreviewViewModel viewModel)
    {
        if (_diffContent == null || viewModel.DiffResult == null) return;

        foreach (var hunk in viewModel.DiffResult.Hunks)
        {
            // Add hunk header
            var headerPanel = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(80, 100, 100, 100)),
                Padding = new Avalonia.Thickness(8, 4),
                Margin = new Avalonia.Thickness(0, 8, 0, 0)
            };

            var headerText = new SelectableTextBlock
            {
                Text = hunk.Header,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(173, 216, 230))  // Much lighter blue
            };

            headerPanel.Child = headerText;
            _diffContent.Children.Add(headerPanel);

            // Add hunk lines
            foreach (var line in hunk.Lines)
            {
                var linePanel = CreateDiffLine(line);
                _diffContent.Children.Add(linePanel);
            }
        }

        if (viewModel.DiffResult.Hunks.Count == 0)
        {
            var noChangesText = new SelectableTextBlock
            {
                Text = "No changes detected",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Margin = new Avalonia.Thickness(20),
                FontSize = 14,
                Foreground = Application.Current?.FindResource("SystemControlForegroundBaseHighBrush") as IBrush ?? 
                           new SolidColorBrush(Color.FromRgb(200, 200, 200))  // Much lighter gray
            };
            _diffContent.Children.Add(noChangesText);
        }
    }

    private void CreateFullFileView(DiffPreviewViewModel viewModel)
    {
        if (_diffContent == null || viewModel.DiffResult == null) return;

        // Create side-by-side view when in full file mode
        var gridPanel = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,4,*")
        };

        // Left side (old file)
        var leftEditor = new TextEditor
        {
            IsReadOnly = true,
            FontFamily = new FontFamily("Consolas"),
            ShowLineNumbers = true,
            WordWrap = false,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            Text = viewModel.OldContent
        };

        // Right side (new file)
        var rightEditor = new TextEditor
        {
            IsReadOnly = true,
            FontFamily = new FontFamily("Consolas"),
            ShowLineNumbers = true,
            WordWrap = false,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            Text = viewModel.NewContent
        };

        // Set syntax highlighting
        var highlighting = GetSyntaxHighlighting(viewModel.FileExtension);
        if (highlighting != null)
        {
            leftEditor.SyntaxHighlighting = highlighting;
            rightEditor.SyntaxHighlighting = highlighting;
        }

        Grid.SetColumn(leftEditor, 0);
        Grid.SetColumn(rightEditor, 2);

        var splitter = new GridSplitter
        {
            ResizeDirection = GridResizeDirection.Columns
        };
        Grid.SetColumn(splitter, 1);

        gridPanel.Children.Add(leftEditor);
        gridPanel.Children.Add(splitter);
        gridPanel.Children.Add(rightEditor);

        _diffContent.Children.Add(gridPanel);
    }

    private Border CreateDiffLine(DiffLine line)
    {
                 var background = line.Type switch
        {
            DiffLineType.Added => new SolidColorBrush(Color.FromArgb(50, 0, 150, 0)),      // Even more subtle green
            DiffLineType.Removed => new SolidColorBrush(Color.FromArgb(50, 150, 0, 0)),    // Even more subtle red
            _ => (IBrush)Brushes.Transparent
        };

        var prefix = line.Type switch
        {
            DiffLineType.Added => "+",
            DiffLineType.Removed => "-",
            _ => " "
        };

        var lineNumberText = line.LineNumber > 0 ? line.LineNumber.ToString().PadLeft(4) : "    ";

        var textBlock = new SelectableTextBlock
        {
            Text = $"{lineNumberText} {prefix} {line.Content}",
            FontFamily = new FontFamily("Consolas"),
            FontSize = 12,
            Padding = new Avalonia.Thickness(4, 1),
            Foreground = line.Type switch
            {
                DiffLineType.Added => new SolidColorBrush(Color.FromRgb(100, 200, 100)),     // Much lighter green
                DiffLineType.Removed => new SolidColorBrush(Color.FromRgb(255, 100, 100)),   // Much lighter red
                _ => Application.Current?.FindResource("SystemControlForegroundBaseHighBrush") as IBrush ?? 
                     new SolidColorBrush(Colors.White)  // Brighter default for dark themes
            },
            TextWrapping = Avalonia.Media.TextWrapping.NoWrap
        };

        return new Border
        {
            Background = background,
            Child = textBlock,
            Name = $"HunkLine_{line.LineNumber}"
        };
    }

    private void OnNavigateToHunk(int hunkIndex)
    {
        if (_diffScrollViewer == null || _diffContent == null || _currentViewModel == null) return;

        if (hunkIndex < 0 || hunkIndex >= _currentViewModel.Hunks.Count) return;

        try
        {
            // Find the hunk header in the UI
            var hunkHeaderIndex = -1;
            for (int i = 0; i < _diffContent.Children.Count; i++)
            {
                if (_diffContent.Children[i] is Border border && border.Child is TextBlock text)
                {
                    if (text.Text?.StartsWith("@@") == true)
                    {
                        hunkHeaderIndex++;
                        if (hunkHeaderIndex == hunkIndex)
                        {
                            // Scroll to this hunk
                            var elementBounds = border.Bounds;
                            var scrollHeight = _diffScrollViewer.Viewport.Height;
                            var targetOffset = Math.Max(0, elementBounds.Y - scrollHeight / 4);
                            
                            _diffScrollViewer.Offset = new Vector(_diffScrollViewer.Offset.X, targetOffset);
                            break;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
        }
    }

    private void OnViewModeChanged()
    {
        if (_currentViewModel != null)
        {
            SetupDiffContent(_currentViewModel);
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (_currentViewModel == null) return;

        switch (e.Key)
        {
            case Key.F7:
                _currentViewModel.PreviousChangeCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.F8:
                _currentViewModel.NextChangeCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Home when e.KeyModifiers == KeyModifiers.None:
                _currentViewModel.FirstChangeCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.F when e.KeyModifiers == KeyModifiers.Control:
                _currentViewModel.ToggleFullFileCommand.Execute(null);
                e.Handled = true;
                break;
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
                ".json" => highlightingManager.GetDefinition("JavaScript"),
                ".lua" => highlightingManager.GetDefinition("JavaScript"),
                ".cfg" or ".ini" => highlightingManager.GetDefinition("INI"),
                ".py" => highlightingManager.GetDefinition("Python"),
                ".cpp" or ".c" or ".h" => highlightingManager.GetDefinition("C++"),
                ".cs" => highlightingManager.GetDefinition("C#"),
                ".js" => highlightingManager.GetDefinition("JavaScript"),
                ".html" or ".htm" => highlightingManager.GetDefinition("HTML"),
                ".css" => highlightingManager.GetDefinition("CSS"),
                ".sql" => highlightingManager.GetDefinition("SQL"),
                ".php" => highlightingManager.GetDefinition("PHP"),
                ".txt" or "" => null,
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }
} 