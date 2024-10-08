using System.IO;
using System.Threading.Tasks;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using StarBreaker.Chf;

namespace StarBreaker.Cli;

[Command("chf-import-watch", Description = "Watch for new characters in the Star Citizen folder and import them.")]
public class WatchImportCommand : ICommand
{
    [CommandOption("input", 'i', Description = "Input folder")]
    public string InputFolder { get; set; } = DefaultPaths.StarCitizenCharactersFolder;
    
    [CommandOption("output", 'o', Description = "Output folder")]
    public required string OutputFolder { get; set; } = DefaultPaths.LocalCharacters;
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        using var watcher = new FileSystemWatcher(InputFolder);
        watcher.NotifyFilter = NotifyFilters.Attributes |
                               NotifyFilters.CreationTime |
                               NotifyFilters.FileName |
                               NotifyFilters.LastAccess |
                               NotifyFilters.LastWrite |
                               NotifyFilters.Size |
                               NotifyFilters.Security;
        watcher.Filter = "*.chf";
        
        watcher.Renamed += async (_, eventArgs) =>
        {
            await console.Output.WriteLineAsync($"New character detected: {eventArgs.FullPath}");
            var fileName = Path.GetFileName(eventArgs.Name);
            if (fileName == null)
            {
                await console.Output.WriteLineAsync("Error: Could not get file name.");
                return;
            }
            var newFilePath = Path.Combine(OutputFolder, fileName);
            await ChfProcessing.ProcessCharacter(newFilePath);
            await console.Output.WriteLineAsync($"Character processed: {newFilePath}");
        };

        watcher.EnableRaisingEvents = true;

        await console.Output.WriteLineAsync("Press enter to stop watching for new characters.");
        await console.Input.ReadLineAsync();
    }
}