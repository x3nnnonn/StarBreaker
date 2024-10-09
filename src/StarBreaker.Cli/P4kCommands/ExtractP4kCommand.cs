using System.Diagnostics;
using System.Text.RegularExpressions;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using StarBreaker.Cli.Utils;
using StarBreaker.P4k;

namespace StarBreaker.Cli.P4kCommands;

[Command("p4k-extract", Description = "Extracts a Game.p4k file")]
public class ExtractP4kCommand : ICommand
{
    [CommandOption("p4k", 'p', Description = "Path to the Game.p4k")]
    public required string P4kFile { get; init; }

    [CommandOption("output", 'o', Description = "Path to the output directory")]
    public required string OutputDirectory { get; init; }

    [CommandOption("filter", 'f', Description = "Pattern to filter entries")]
    public string? FilterPattern { get; init; }

    public ValueTask ExecuteAsync(IConsole console)
    {
        var p4k = new P4kFile(P4kFile);

        console.Output.WriteLine("DataForge loaded.");
        console.Output.WriteLine("Exporting...");

        var sw = Stopwatch.StartNew();
        p4k.Extract(OutputDirectory, FilterPattern, new ProgressBar(console));
        sw.Stop();

        console.Output.WriteLine();
        console.Output.WriteLine($"Export completed in {sw.ElapsedMilliseconds}ms.");

        return default;
    }
}