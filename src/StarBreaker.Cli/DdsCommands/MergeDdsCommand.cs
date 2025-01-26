using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using StarBreaker.Dds;

namespace StarBreaker.Cli;

[Command("dds-merge", Description = "Merges multiple DDS files into a single DDS file")]
public class MergeDdsCommand : ICommand
{
    [CommandOption("input", 'i', Description = "Input DDS file. Must be main *.dds file", EnvironmentVariable = "INPUT_FILE")]
    public required string Input { get; init; }

    [CommandOption("output", 'o', Description = "Output DDS file", EnvironmentVariable = "OUTPUT_FILE")]
    public required string Output { get; init; }

    public ValueTask ExecuteAsync(IConsole console)
    {
        if (string.IsNullOrWhiteSpace(Input))
        {
            console.Error.WriteLine("Input file is required");
            return default;
        }

        if (string.IsNullOrWhiteSpace(Output))
        {
            console.Error.WriteLine("Output file is required");
            return default;
        }

        var outFileDir = Path.GetDirectoryName(Output);
        if (!string.IsNullOrWhiteSpace(outFileDir) && !Directory.Exists(outFileDir))
            Directory.CreateDirectory(outFileDir);

        if (!File.Exists(Input))
        {
            console.Error.WriteLine("Input file not found");
            return default;
        }

        if (!Input.EndsWith(".dds"))
        {
            console.Error.WriteLine("Input file must be a DDS file");
            return default;
        }

        DdsFile.MergeToFile(Input, Output);

        return default;
    }
}