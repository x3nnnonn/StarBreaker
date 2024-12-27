using System.Diagnostics;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using StarBreaker.Cli.Utils;
using StarBreaker.DataCore;
using StarBreaker.P4k;

namespace StarBreaker.Cli.DataCoreCommands;

[Command("dcb-extract", Description = "Extracts a DataCore binary file into separate xml files")]
public class DataCoreExtractCommand : ICommand
{
    [CommandOption("p4k", 'p', Description = "Path to the Game.p4k")]
    public required string P4kFile { get; init; }
    
    [CommandOption("output", 'o', Description = "Path to the output directory")]
    public required string OutputDirectory { get; init; }
    
    [CommandOption("filter", 'f', Description = "Pattern to filter entries")]
    public string? Filter { get; init; }
    
    public ValueTask ExecuteAsync(IConsole console)
    {
        var p4k = P4k.P4kFile.FromFile(P4kFile);
        console.Output.WriteLine("P4k loaded.");
        var dcbStream = p4k.OpenRead(@"Data\Game2.dcb");
        console.Output.WriteLine("DataCore extracted.");

        var df = new DataForge(dcbStream);

        console.Output.WriteLine("DataCore loaded.");
        console.Output.WriteLine("Exporting...");

        var sw = Stopwatch.StartNew();
        df.ExtractAllParallel(OutputDirectory, Filter, new ProgressBar(console));
        sw.Stop();
        
        console.Output.WriteLine();
        console.Output.WriteLine($"Export completed in {sw.ElapsedMilliseconds}ms.");
        
        return default;
    }
}