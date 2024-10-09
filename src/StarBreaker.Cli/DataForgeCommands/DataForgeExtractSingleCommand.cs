using System.Diagnostics;
using System.Text.RegularExpressions;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using StarBreaker.Cli.Utils;
using StarBreaker.Forge;

namespace StarBreaker.Cli.DataForgeCommands;

[Command("df-extract-single", Description = "Extracts a DataForge binary file into a single xml")]
public class DataForgeExtractSingleCommand : ICommand
{
    [CommandOption("dcb", 'd', Description = "Path to the DataForge binary file")]
    public required string DataForgeBinary { get; init; }
    
    [CommandOption("output", 'o', Description = "Path to the output directory")]
    public required string OutputDirectory { get; init; }
    
    [CommandOption("filter", 'f', Description = "Pattern to filter entries")]
    public string? Filter { get; init; }
    
    public ValueTask ExecuteAsync(IConsole console)
    {
        var dataForge = new DataForge(DataForgeBinary);

        console.Output.WriteLine("DataForge loaded.");
        console.Output.WriteLine("Exporting...");
        
        var sw = Stopwatch.StartNew();
        dataForge.ExtractSingle(OutputDirectory, Filter, new ProgressBar(console));
        sw.Stop();
        
        console.Output.WriteLine();
        console.Output.WriteLine($"Export completed in {sw.ElapsedMilliseconds}ms.");
        
        return default;
    }
}