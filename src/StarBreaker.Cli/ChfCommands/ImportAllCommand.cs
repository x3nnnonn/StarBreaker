using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using StarBreaker.Chf;

namespace StarBreaker.Cli;

[Command("chf-import-all", Description = "Imports all non-modded characters exported from the game into our local characters folder.")]
public class ImportAllCommand : ICommand
{
    [CommandOption("output", 'o', Description = "Output folder")]
    public required string OutputFolder { get; init; }

    [CommandOption("input", 'i', Description = "Input folder")]
    public required string InputFolder { get; init; }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        if (!Directory.Exists(InputFolder))
        {
            await console.Error.WriteLineAsync("CustomCharacters folder not found");
            return;
        }

        var characters = Directory.GetFiles(InputFolder, "*.chf", SearchOption.AllDirectories);

        foreach (var character in characters)
        {
            var name = Path.GetFileNameWithoutExtension(character);
            var output = Path.Combine(OutputFolder, name);

            if (Directory.Exists(output))
                continue;

            var chf = ChfFile.FromChf(character);

            //do not import our own characters again.
            if (chf.Modded)
                continue;

            Directory.CreateDirectory(output);
            await chf.WriteToChfFileAsync(Path.Combine(output, $"{name}.chf"));
        }
    }
}