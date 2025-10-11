using System.Diagnostics;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using StarBreaker.Cli.Utils;
using StarBreaker.P4k;

namespace StarBreaker.Cli;

[Command("p4k-extract", Description = "Extracts a Game.p4k file")]
public class ExtractP4kCommand : ICommand
{
    [CommandOption("p4k", 'p', Description = "Path to the Game.p4k", EnvironmentVariable = "INPUT_P4K")]
    public required string P4kFile { get; init; }

    [CommandOption("output", 'o', Description = "Path to the output directory", EnvironmentVariable = "OUTPUT_FOLDER")]
    public required string OutputDirectory { get; init; }

    [CommandOption("filter", 'f', Description = "Pattern to filter entries", EnvironmentVariable = "FILTER")]
    public string? FilterPattern { get; init; }

    public ValueTask ExecuteAsync(IConsole console)
    {
        var p4k = P4k.P4kFile.FromFile(P4kFile);

        console.Output.WriteLine("DataCore loaded.");
        console.Output.WriteLine("Exporting...");

        var sw = Stopwatch.StartNew();
        var extractor = new P4kExtractor(p4k);
        extractor.ExtractFiltered(OutputDirectory, FilterPattern, new ProgressBar(console));
        sw.Stop();

        console.Output.WriteLine();
        console.Output.WriteLine($"Export completed in {sw}");

        return default;
    }
}