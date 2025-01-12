using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using StarBreaker.P4k;

namespace StarBreaker.Cli;

[Command("p4k-dump", Description = "Dumps the contents a Game.p4k file")]
public class DumpP4kCommand : ICommand
{
    [CommandOption("p4k", 'p', Description = "Path to the Game.p4k")]
    public required string P4kFile { get; init; }

    [CommandOption("output", 'o', Description = "Path to the output directory")]
    public required string OutputDirectory { get; init; }

    public ValueTask ExecuteAsync(IConsole console)
    {
        var p4k = P4k.P4kFile.FromFile(P4kFile);
        var p4kExtractor = new P4kExtractor(p4k);
        p4kExtractor.ExtractDummies(OutputDirectory);

        return default;
    }
}