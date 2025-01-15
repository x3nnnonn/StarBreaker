using System.IO.Compression;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using StarBreaker.DataCore;
using StarBreaker.P4k;

namespace StarBreaker.Cli;

[Command("diff", Description = "Dumps game information into plain text files for comparison")]
public class DiffCommand : ICommand
{
    [CommandOption("game", 'g', Description = "Path to the game folder")]
    public required string GameFolder { get; init; }

    [CommandOption("output", 'o', Description = "Path to the output directory")]
    public required string OutputDirectory { get; init; }

    [CommandOption("keep", 'k', Description = "Keep old files in the output directory")]
    public bool KeepOld { get; init; }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        if (!KeepOld)
        {
            await console.Output.WriteLineAsync("Deleting old files...");
            string[] deleteFolder =
            [
                Path.Combine(OutputDirectory, "Binaries"),
                Path.Combine(OutputDirectory, "P4k"),
                Path.Combine(OutputDirectory, "DataCore"),
                Path.Combine(OutputDirectory, "Protobuf"),
            ];
            string[] deleteFile =
            [
                Path.Combine(OutputDirectory, "build_manifest.json"),
            ];

            foreach (var folder in deleteFolder)
            {
                if (Directory.Exists(folder))
                    Directory.Delete(folder, true);
            }

            foreach (var file in deleteFile)
            {
                if (File.Exists(file))
                    File.Delete(file);
            }

            await console.Output.WriteLineAsync("Old files deleted.");
        }

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
        await console.Output.WriteLineAsync("P4k dumped.");

        var dcbExtract = new DataCoreExtractCommand
        {
            P4kFile = p4kFile,
            OutputDirectory = Path.Combine(OutputDirectory, "DataCore")
        };
        await dcbExtract.ExecuteAsync(fakeConsole);
        await console.Output.WriteLineAsync("DataCore extracted.");

        var extractProtobufs = new ExtractProtobufsCommand
        {
            Input = exeFile,
            Output = Path.Combine(OutputDirectory, "Protobuf")
        };
        await extractProtobufs.ExecuteAsync(fakeConsole);
        await console.Output.WriteLineAsync("Protobufs extracted.");

        var extractDescriptor = new ExtractDescriptorSetCommand
        {
            Input = exeFile,
            Output = Path.Combine(OutputDirectory, "Protobuf", "descriptor_set.bin")
        };
        await extractDescriptor.ExecuteAsync(fakeConsole);
        await console.Output.WriteLineAsync("Descriptor set extracted.");

        await ExtractDataCoreIntoZip(p4kFile, Path.Combine(OutputDirectory, "DataCore", "DataCore.zip"));
        await ExtractExecutableIntoZip(exeFile, Path.Combine(OutputDirectory, "Binaries", "StarCitizen.zip"));
        File.Copy(Path.Combine(GameFolder, "build_manifest.id"), Path.Combine(OutputDirectory, "build_manifest.json"), true);
        await console.Output.WriteLineAsync("Zipped DataCore and StarCitizen.");

        await console.Output.WriteLineAsync("Done.");
    }

    private static async Task ExtractDataCoreIntoZip(string p4kFile, string zipPath)
    {
        var p4k = new P4kFileSystem(P4kFile.FromFile(p4kFile));
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

        Directory.CreateDirectory(Path.GetDirectoryName(zipPath)!);
        using var zip = new ZipArchive(File.Create(zipPath), ZipArchiveMode.Create);
        var entry = zip.CreateEntry(Path.GetFileName(dcbFile), CompressionLevel.SmallestSize);
        await using var entryStream = entry.Open();
        await dcbStream.CopyToAsync(entryStream);
    }

    private static async Task ExtractExecutableIntoZip(string exeFile, string zipPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(zipPath)!);
        using var zip = new ZipArchive(File.Create(zipPath), ZipArchiveMode.Create);
        var entry = zip.CreateEntry(Path.GetFileName(exeFile), CompressionLevel.SmallestSize);
        await using var entryStream = entry.Open();
        await using var exeStream = File.OpenRead(exeFile);
        await exeStream.CopyToAsync(entryStream);
    }
}