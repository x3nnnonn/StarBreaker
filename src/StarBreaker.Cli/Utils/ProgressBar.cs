using CliFx.Infrastructure;

namespace StarBreaker.Cli.Utils;

public class ProgressBar : IProgress<double>
{
    private const int _totalBlocks = 20;
    private readonly IConsole _console;
    private int? _originalCursorLeft;
    private int? _originalCursorTop;
    
    public ProgressBar(IConsole console)
    {
        _console = console;
    }

    private void RenderProgress(double progress)
    {
        if (_originalCursorLeft != null && _originalCursorTop != null)
        {
            _console.CursorLeft = _originalCursorLeft.Value;
            _console.CursorTop = _originalCursorTop.Value;
        }
        else
        {
            _originalCursorLeft = _console.CursorLeft;
            _originalCursorTop = _console.CursorTop;
        }

        var completedBlocks = (int)(progress * _totalBlocks);
        var progressBar = new string('#', completedBlocks) + new string('-', _totalBlocks - completedBlocks);
        
        _console.Output.WriteLine($"[{progressBar}] {progress:P0}");
    }

    public void Report(double progress)
    {
        var setCursorPosition = _originalCursorLeft != null && _originalCursorTop != null;
        if (!_console.IsOutputRedirected)
        {
            var position = (_console.CursorLeft, _console.CursorTop);
            
            RenderProgress(progress);

            if (setCursorPosition)
            {
                _console.CursorLeft = position.CursorLeft;
                _console.CursorTop = position.CursorTop;
            }
        }
    }
}