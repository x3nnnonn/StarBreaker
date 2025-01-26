using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using StarBreaker.Chf;

namespace StarBreaker.Cli;

[Command("chf-export-all", Description = "Exports all modded characters into the Star Citizen folder.")]
public class ExportAllCommand : ICommand
{
    [CommandOption("input", 'i', Description = "Input folder", EnvironmentVariable = "INPUT_FOLDER")]
    public required string InputFolder { get; init; }

    [CommandOption("output", 'o', Description = "Output folder", EnvironmentVariable = "OUTPUT_FOLDER")]
    public required string OutputFolder { get; init; }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        if (!Directory.Exists(InputFolder))
        {
            await console.Error.WriteLineAsync($"{InputFolder} not found");
            return;
        }

        var bins = Directory.GetFiles(InputFolder, "*.bin", SearchOption.AllDirectories);
        await Task.WhenAll(bins.Select(async b =>
        {
            var target = Path.Combine(OutputFolder, Path.ChangeExtension(b, ".chf"));
            if (File.Exists(target))
                return;

            var file = ChfFile.FromBin(b);

            await console.Output.WriteLineAsync($"Exporting {Path.GetFileNameWithoutExtension((string?)b)}");
            await file.WriteToChfFileAsync(target);
        }));
    }
}