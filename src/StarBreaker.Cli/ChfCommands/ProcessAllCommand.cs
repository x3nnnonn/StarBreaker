using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using StarBreaker.Chf;
using StarBreaker.Cli.Utils;

namespace StarBreaker.Cli;

[Command("chf-process-all", Description = "Processes all characters in the given folder.")]
public class ProcessAllCommand : ICommand
{
    [CommandOption("input", 'i', Description = "Input Folder to process.")]
    public required string InputFolder { get; init; }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        if (!Directory.Exists(InputFolder))
        {
            await console.Error.WriteLineAsync("Folder not found");
            return;
        }

        var progress = new ProgressBar(console);
        var files = Directory.GetFiles(InputFolder, "*.chf", SearchOption.AllDirectories);
        var processedFiles = 0;
        
        progress.Report(0);
        
        foreach (var characterFile in files)
        {
            try
            {
                await ChfProcessing.ProcessCharacter(characterFile);
                processedFiles++;
                progress.Report((double)processedFiles / files.Length);
            }
            catch (Exception e)
            {
                await console.Error.WriteLineAsync($"Error processing {characterFile}: {e.Message}");
            }
        }
        
        progress.Report(1);
    }
}