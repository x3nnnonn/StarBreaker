using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using StarBreaker.Cli.Utils;
using StarBreaker.Dds;

namespace StarBreaker.Cli;

[Command("dds-merge-all", Description = "Merges all DDS files in a folder into a single DDS file")]
public class MergeAllDdsCommand : ICommand
{
    [CommandOption("input", 'i', Description = "Input folder", EnvironmentVariable = "INPUT_FOLDER")]
    public required string Input { get; init; }

    [CommandOption("output", 'o', Description = "Output DDS folder", EnvironmentVariable = "OUTPUT_FOLDER")]
    public required string Output { get; init; }

    public ValueTask ExecuteAsync(IConsole console)
    {
        if (string.IsNullOrWhiteSpace(Input))
        {
            console.Error.WriteLine("Input folder is required");
            return default;
        }

        if (string.IsNullOrWhiteSpace(Output))
        {
            console.Error.WriteLine("Output file is required");
            return default;
        }

        if (!Directory.Exists(Input))
        {
            console.Error.WriteLine("Input folder not found");
            return default;
        }

        var files = Directory.GetFiles(Input, "*.dds", SearchOption.AllDirectories);
        if (files.Length == 0)
        {
            console.Error.WriteLine("No DDS files found");
            return default;
        }

        var outFileDir = Path.GetDirectoryName(Output);
        if (!string.IsNullOrWhiteSpace(outFileDir) && !Directory.Exists(outFileDir))
            Directory.CreateDirectory(outFileDir);

        var progress = new ProgressBar(console);
        var processedFiles = 0;

        //TODO: parallelize
        progress.Report(0);
        foreach (var file in files)
        {
            var output = Path.Combine(Output, Path.GetRelativePath(Input, file));
            var path = Path.GetDirectoryName(output);
            if (!string.IsNullOrWhiteSpace(path) && !Directory.Exists(path))
                Directory.CreateDirectory(path);


            DdsFile.MergeToFile(file, output);
            processedFiles++;
            progress.Report((double)processedFiles / files.Length);
        }

        progress.Report(1);

        console.Output.WriteLine($"Merged {processedFiles} DDS files into {Output}");

        return default;
    }
}

//TODO: to png