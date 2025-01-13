using System.Diagnostics;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using StarBreaker.Cli.Utils;
using StarBreaker.DataCore;
using StarBreaker.P4k;

namespace StarBreaker.Cli;

[Command("dcb-extract", Description = "Extracts a DataCore binary file into separate xml files")]
public class DataCoreExtractCommand : ICommand
{
    [CommandOption("p4k", 'p', Description = "Path to the Game.p4k")]
    public string? P4kFile { get; init; }

    [CommandOption("dcb", 'd', Description = "Path to the Game.dcb")]
    public string? DcbFile { get; init; }
    
    [CommandOption("output", 'o', Description = "Path to the output directory")]
    public required string OutputDirectory { get; init; }
    
    [CommandOption("filter", 'f', Description = "Pattern to filter entries")]
    public string? Filter { get; init; }
    
    public ValueTask ExecuteAsync(IConsole console)
    {
        if (P4kFile == null && DcbFile == null)
        {
            console.Output.WriteLine("P4k and DCB files are required.");
            return default;
        }
        if (!string.IsNullOrEmpty(P4kFile) && !string.IsNullOrEmpty(DcbFile))
        {
            console.Output.WriteLine("Only one of P4k and DCB files can be specified.");
            return default;
        }

        Stream? dcbStream = null;
        if (!string.IsNullOrEmpty(DcbFile))
        {
            dcbStream = File.OpenRead(DcbFile);
            console.Output.WriteLine("DCB loaded.");
        }
        else if (!string.IsNullOrEmpty(P4kFile))
        {
            var p4k = new P4kFileSystem(P4k.P4kFile.FromFile(P4kFile));
            console.Output.WriteLine("P4k loaded.");
            foreach (var file in DataCoreUtils.KnownPaths)
            {
                if (!p4k.FileExists(file)) continue;

                dcbStream = p4k.OpenRead(file);
                console.Output.WriteLine($"{file} found");
                break;
            }
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