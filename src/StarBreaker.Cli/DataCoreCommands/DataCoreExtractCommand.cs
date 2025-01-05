using System.Diagnostics;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using StarBreaker.Cli.Utils;
using StarBreaker.DataCore;

namespace StarBreaker.Cli;

[Command("dcb-extract", Description = "Extracts a DataCore binary file into separate xml files")]
public class DataCoreExtractCommand : ICommand
{
    private static readonly string[] _dataCoreFiles = [@"Data\Game2.dcb", @"Data\Game.dcb"];

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
        Stream? dcbStream = null;
        foreach (var file in _dataCoreFiles)
        {
            if (!p4k.FileExists(file)) continue;

            dcbStream = p4k.OpenRead(file);
            console.Output.WriteLine($"{file} found");
            break;
        }

        if (dcbStream == null)
        {
            console.Output.WriteLine("DataCore not found.");
            return default;
        }

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