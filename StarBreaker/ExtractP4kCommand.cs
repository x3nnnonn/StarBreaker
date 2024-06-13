using System.Diagnostics;
using System.Text.RegularExpressions;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace StarBreaker;

[Command("extract-p4k", Description = "Extracts a Game.p4k file")]
public class ExtractP4kCommand : ICommand
{
    [CommandOption("p4k", 'p', Description = "Path to the Game.p4k")]
    public required string P4kFile { get; init; }
    
    [CommandOption("output", 'o', Description = "Path to the output directory")]
    public required string OutputDirectory { get; init; }
    
    [CommandOption("filter", 'f', Description = "Regex pattern to filter entries")]
    public Regex? RegexPattern { get; init; }
    
    public ValueTask ExecuteAsync(IConsole console)
    {
        var p4k = new P4k.Unp4ker(P4kFile, OutputDirectory);

        console.Output.WriteLine("DataForge loaded.");
        console.Output.WriteLine("Exporting...");
        
        var sw = Stopwatch.StartNew();
        p4k.Extract(RegexPattern, new ProgressBar(console));
        sw.Stop();
        
        console.Output.WriteLine();
        console.Output.WriteLine($"Export completed in {sw.ElapsedMilliseconds}ms.");
        
        return default;
    }
}