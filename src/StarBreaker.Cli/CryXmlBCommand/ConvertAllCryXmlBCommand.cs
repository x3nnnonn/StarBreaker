using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using StarBreaker.CryXmlB;

namespace StarBreaker.Cli;

[Command("cryxml-convert-all", Description = "Converts all CryXmlB files in a folder to XML files")]
public sealed class ConvertAllCryXmlBCommand : ICommand
{
    [CommandOption("input", 'i', Description = "Input folder", EnvironmentVariable = "INPUT_FOLDER")]
    public required string? Input { get; set; }

    [CommandOption("output", 'o', Description = "Output folder", EnvironmentVariable = "OUTPUT_FOLDER")]
    public required string? Output { get; set; }
    
    [CommandOption("filter", 'f', Description = "File filter", EnvironmentVariable = "FILTER")]
    public string Filter { get; set; } = "*.xml";

    public ValueTask ExecuteAsync(IConsole console)
    {
        if (string.IsNullOrWhiteSpace(Input))
        {
            console.Error.WriteLine("Input folder is required");
            return default;
        }

        if (string.IsNullOrWhiteSpace(Output))
        {
            console.Error.WriteLine("Output folder is required");
            return default;
        }

        if (!Directory.Exists(Input))
        {
            console.Error.WriteLine("Input folder not found");
            return default;
        }
        
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var files = Directory.GetFiles(Input, Filter, SearchOption.AllDirectories);
        Parallel.ForEach(files, file =>
        {
            var output = Path.Combine(Output, Path.GetRelativePath(Input, file));
            var path = Path.GetDirectoryName(output);
            if (!string.IsNullOrWhiteSpace(path) && !Directory.Exists(path))
                Directory.CreateDirectory(path);

            if (!CryXml.TryOpen(File.OpenRead(file), out var cryXml))
            {
                console.Error.WriteLine($"Invalid CryXmlB file: {file}");
                return;
            }

            cryXml.Save(output);
        });
        
        console.Output.WriteLine($"Converted {files.Length} files in {sw.Elapsed}");

        return default;
    }
}