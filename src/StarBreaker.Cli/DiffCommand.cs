using System.Diagnostics;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using StarBreaker.DataCore;
using StarBreaker.P4k;
using ZstdSharp;

namespace StarBreaker.Cli;

[Command("diff", Description = "Dumps game information into plain text files for comparison")]
public class DiffCommand : ICommand
{
    [CommandOption("game", 'g', Description = "Path to the game folder", EnvironmentVariable = "GAME_FOLDER")]
    public required string GameFolder { get; init; }

    [CommandOption("output", 'o', Description = "Path to the output directory", EnvironmentVariable = "OUTPUT_FOLDER")]
    public required string OutputDirectory { get; init; }

    [CommandOption("keep", 'k', Description = "Keep old files in the output directory", EnvironmentVariable = "KEEP_OLD")]
    public bool KeepOld { get; init; }

    [CommandOption("include-binaries", 'b', Description = "Include DataCore.dcb.zst and StarCitizen.exe.zst in the output", EnvironmentVariable = "INCLUDE_BINARIES")]
    public bool IncludeBinaries { get; init; } = false;

    [CommandOption("format", 'f', Description = "Output format", EnvironmentVariable = "TEXT_FORMAT")]
    public string TextFormat { get; init; } = "xml";

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var swTotal = Stopwatch.StartNew();
        var sw = Stopwatch.StartNew();

        if (!KeepOld)
        {
            await console.Output.WriteLineAsync("Deleting old files...");
            List<string> deleteFolder =
            [
                Path.Combine(OutputDirectory, "DataCore"),
                Path.Combine(OutputDirectory, "DataCoreTypes"),
                Path.Combine(OutputDirectory, "DataCoreEnums"),
                Path.Combine(OutputDirectory, "P4k"),
                Path.Combine(OutputDirectory, "P4kContents"),
            ];

            List<string> deleteFile =
            [
                Path.Combine(OutputDirectory, "build_manifest.json"),
            ];
            
            if (IncludeBinaries)
            {
                deleteFile.Add(Path.Combine(OutputDirectory, "DataCore.dcb.zst"));
                deleteFile.Add(Path.Combine(OutputDirectory, "StarCitizen.exe.zst"));
            }

            foreach (var folder in deleteFolder.Where(Directory.Exists))
                Directory.Delete(folder, true);

            foreach (var file in deleteFile.Where(File.Exists))
                File.Delete(file);

            await console.Output.WriteLineAsync("Old files deleted in " + sw.Elapsed);
            sw.Restart();
        }

        // Hide output from subcommands
        var fakeConsole = new FakeConsole();

        var p4kFile = Path.Combine(GameFolder, "Data.p4k");
        var exeFile = Path.Combine(GameFolder, "Bin64", "StarCitizen.exe");

        var dumpP4k = new DumpP4kCommand
        {
            P4kFile = p4kFile,
            OutputDirectory = Path.Combine(OutputDirectory, "P4k"),
            TextFormat = TextFormat,
        };
        await dumpP4k.ExecuteAsync(fakeConsole);
        await console.Output.WriteLineAsync("P4k dumped in " + sw.Elapsed);
        sw.Restart();

        string[] p4kContentsToExtract =
        [
            "*english\\global.ini",
            "*TagDatabase.TagDatabase.xml",
        ];

        foreach (var p4kContentFilter in p4kContentsToExtract)
        {
            var localizationDump = new ExtractP4kCommand
            {
                P4kFile = p4kFile,
                OutputDirectory = Path.Combine(OutputDirectory, "P4kContents"),
                FilterPattern = p4kContentFilter,
            };
            await localizationDump.ExecuteAsync(fakeConsole);
        }
        
        await console.Output.WriteLineAsync("P4k contents extracted in " + sw.Elapsed);
        sw.Restart();

        var dcbExtract = new DataCoreExtractCommand
        {
            P4kFile = p4kFile,
            OutputDirectory = Path.Combine(OutputDirectory, "DataCore"),
            OutputFolderTypes = Path.Combine(OutputDirectory, "DataCoreTypes"),
            OutputFolderEnums = Path.Combine(OutputDirectory, "DataCoreEnums"),
            TextFormat = TextFormat
        };
        await dcbExtract.ExecuteAsync(fakeConsole);
        await console.Output.WriteLineAsync("DataCore extracted in " + sw.Elapsed);
        sw.Restart();

        var extractProtobufs = new ExtractProtobufsCommand
        {
            Input = exeFile,
            Output = Path.Combine(OutputDirectory, "Protobuf")
        };
        await extractProtobufs.ExecuteAsync(fakeConsole);
        await console.Output.WriteLineAsync("Protobuf definitions extracted in " + sw.Elapsed);
        sw.Restart();

        var extractDescriptor = new ExtractDescriptorSetCommand
        {
            Input = exeFile,
            Output = Path.Combine(OutputDirectory, "Protobuf", "descriptor_set.bin")
        };
        await extractDescriptor.ExecuteAsync(fakeConsole);
        await console.Output.WriteLineAsync("Protobuf descriptor set extracted in " + sw.Elapsed);
        sw.Restart();

        if (IncludeBinaries)
        {
            await ExtractDataCoreIntoZip(p4kFile, Path.Combine(OutputDirectory, "DataCore.dcb.zst"));
            await ExtractExecutableIntoZip(exeFile, Path.Combine(OutputDirectory, "StarCitizen.exe.zst"));
            await console.Output.WriteLineAsync("Binaries extracted in " + sw.Elapsed);
            sw.Restart();
        }

        File.Copy(Path.Combine(GameFolder, "build_manifest.id"), Path.Combine(OutputDirectory, "build_manifest.json"), true);

        await console.Output.WriteLineAsync($"Done in {swTotal.Elapsed}");
    }

    private static async Task ExtractDataCoreIntoZip(string p4kFile, string zipPath)
    {
        var p4k = P4kDirectoryNode.FromP4k(P4kFile.FromFile(p4kFile));
        Stream? input = null;
        foreach (var file in DataCoreUtils.KnownPaths)
        {
            if (!p4k.FileExists(file)) continue;

            input = p4k.OpenRead(file);
            break;
        }

        if (input == null)
            throw new InvalidOperationException("DataCore not found.");

        await using var output = File.OpenWrite(zipPath);
        await using var compressionStream = new CompressionStream(output, leaveOpen: false);
        await input.CopyToAsync(compressionStream);
    }

    private static async Task ExtractExecutableIntoZip(string exeFile, string zipPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(zipPath)!);
        await using var input = File.OpenRead(exeFile);
        await using var output = File.OpenWrite(zipPath);

        await using var compressionStream = new CompressionStream(output, leaveOpen: false);
        await input.CopyToAsync(compressionStream);
    }
}