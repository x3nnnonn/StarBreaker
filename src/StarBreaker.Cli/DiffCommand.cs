using System.IO.Compression;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using StarBreaker.DataCore;

namespace StarBreaker.Cli;

[Command("diff", Description = "Dumps game information into plain text files for comparison")]
public class DiffCommand : ICommand
{
    [CommandOption("game", 'g', Description = "Path to the game folder")]
    public required string GameFolder { get; init; }

    [CommandOption("output", 'o', Description = "Path to the output directory")]
    public required string OutputDirectory { get; init; }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        // Hide output from subcommands
        var fakeConsole = new FakeConsole();

        var p4kFile = Path.Combine(GameFolder, "Data.p4k");
        var exeFile = Path.Combine(GameFolder, "Bin64", "StarCitizen.exe");

        var dumpP4k = new DumpP4kCommand
        {
            P4kFile = p4kFile,
            OutputDirectory = Path.Combine(OutputDirectory, "P4k")
        };
        await dumpP4k.ExecuteAsync(fakeConsole);

        var dcbExtract = new DataCoreExtractCommand
        {
            P4kFile = p4kFile,
            OutputDirectory = Path.Combine(OutputDirectory, "DataCore")
        };
        await dcbExtract.ExecuteAsync(fakeConsole);

        var extractProtobufs = new ExtractProtobufsCommand
        {
            Input = exeFile,
            Output = Path.Combine(OutputDirectory, "Protobuf")
        };
        await extractProtobufs.ExecuteAsync(fakeConsole);

        var extractDescriptor = new ExtractDescriptorSetCommand
        {
            Input = exeFile,
            Output = Path.Combine(OutputDirectory, "Protobuf", "descriptor_set.bin")
        };
        await extractDescriptor.ExecuteAsync(fakeConsole);

        await ExtractDataCoreIntoZip(p4kFile, Path.Combine(OutputDirectory, "DataCore", "DataCore.zip"));

        await console.Output.WriteLineAsync("Done.");
    }

    private static async Task ExtractDataCoreIntoZip(string p4kFile, string zipPath)
    {
        var p4k = P4k.P4kFile.FromFile(p4kFile);
        Stream? dcbStream = null;
        string? dcbFile = null;
        foreach (var file in DataCoreUtils.KnownPaths)
        {
            if (!p4k.FileExists(file)) continue;

            dcbFile = file;
            dcbStream = p4k.OpenRead(file);
            break;
        }

        if (dcbStream == null || dcbFile == null)
            throw new InvalidOperationException("DataCore not found.");

        using var zip = new ZipArchive(File.Create(zipPath), ZipArchiveMode.Create);
        var entry = zip.CreateEntry(Path.GetFileName(dcbFile), CompressionLevel.SmallestSize);
        await using var entryStream = entry.Open();
        await dcbStream.CopyToAsync(entryStream);
    }
}