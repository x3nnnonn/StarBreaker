using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using StarBreaker.Cli;
using StarBreaker.Cli.Utils;

namespace StarBreaker.Chf;

[Command("chf-process-all", Description = "Processes all characters in the given folder.")]
public class ProcessAllCommand : ICommand
{
    [CommandOption("input", 'i', Description = "Input Folder to process.")]
    public string InputFolder { get; set; } = Path.Combine(DefaultPaths.ResearchFolder);

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