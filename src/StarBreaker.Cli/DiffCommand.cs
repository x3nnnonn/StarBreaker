using System.Diagnostics;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using StarBreaker.CryXmlB;
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
                Path.Combine(OutputDirectory, "Localization"),
                Path.Combine(OutputDirectory, "TagDatabase"),
                Path.Combine(OutputDirectory, "Protobuf"),
            ];

            List<string> deleteFile =
            [
                Path.Combine(OutputDirectory, "build_manifest.json"),
                Path.Combine(OutputDirectory, "DataCore.dcb.zst"),
                Path.Combine(OutputDirectory, "StarCitizen.exe.zst"),
            ];

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

        await ExtractLocalization(p4kFile, console);
        await console.Output.WriteLineAsync("Localization extracted in " + sw.Elapsed);
        sw.Restart();

        await ExtractTagDatabase(p4kFile, console);
        await console.Output.WriteLineAsync("TagDatabase extracted in " + sw.Elapsed);
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

        await ExtractP4kXmlFiles(p4kFile, console);
        await console.Output.WriteLineAsync("P4K XML files extracted in " + sw.Elapsed);
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

        await ExtractDataCoreIntoZip(p4kFile, Path.Combine(OutputDirectory, "DataCore.dcb.zst"));
        await ExtractExecutableIntoZip(exeFile, Path.Combine(OutputDirectory, "StarCitizen.exe.zst"));
        await console.Output.WriteLineAsync("Compressed archives created in " + sw.Elapsed);
        sw.Restart();

        var buildManifestSource = Path.Combine(GameFolder, "build_manifest.id");
        var buildManifestTarget = Path.Combine(OutputDirectory, "build_manifest.json");
        if (File.Exists(buildManifestSource))
        {
            File.Copy(buildManifestSource, buildManifestTarget, true);
        }

        await console.Output.WriteLineAsync($"Done in {swTotal.Elapsed}");
    }

    private async Task ExtractLocalization(string p4kFile, IConsole console)
    {
        var p4kFileSystem = new P4kFileSystem(P4kFile.FromFile(p4kFile));
        var outputDir = Path.Combine(OutputDirectory, "Localization");

        string[] localizationPaths = [
            "Data/Localization/english/global.ini",
            "Data\\Localization\\english\\global.ini"
        ];

        foreach (var path in localizationPaths)
        {
            if (p4kFileSystem.FileExists(path))
            {
                using var stream = p4kFileSystem.OpenRead(path);
                var outputPath = Path.Combine(outputDir, Path.GetFileName(path));
                Directory.CreateDirectory(outputDir);
                
                await using var outputFile = File.Create(outputPath);
                await stream.CopyToAsync(outputFile);
                
                await console.Output.WriteLineAsync($"Extracted: {Path.GetFileName(path)}");
                return;
            }
        }

        await console.Output.WriteLineAsync("Localization file not found.");
    }

    private async Task ExtractTagDatabase(string p4kFile, IConsole console)
    {
        var p4k = P4kFile.FromFile(p4kFile);
        var outputDir = Path.Combine(OutputDirectory, "TagDatabase");

        var tagDbEntry = p4k.Entries.FirstOrDefault(e => 
            e.Name.Contains("TagDatabase.TagDatabase.xml", StringComparison.OrdinalIgnoreCase));

        if (tagDbEntry == null)
        {
            await console.Output.WriteLineAsync("TagDatabase not found in P4K.");
            return;
        }

        using var entryStream = p4k.OpenStream(tagDbEntry);
        var ms = new MemoryStream();
        entryStream.CopyTo(ms);
        ms.Position = 0;

        var entryPath = Path.Combine(outputDir, tagDbEntry.RelativeOutputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(entryPath)!);

        if (CryXml.IsCryXmlB(ms))
        {
            ms.Position = 0;
            if (CryXml.TryOpen(ms, out var cryXml))
            {
                File.WriteAllText(entryPath, cryXml.ToString());
                await console.Output.WriteLineAsync("TagDatabase extracted and converted from CryXML.");
            }
            else
            {
                using var fs = File.Create(entryPath);
                ms.Position = 0;
                ms.CopyTo(fs);
                await console.Output.WriteLineAsync("TagDatabase extracted (binary format).");
            }
        }
        else
        {
            using var fs = File.Create(entryPath);
            ms.Position = 0;
            ms.CopyTo(fs);
            await console.Output.WriteLineAsync("TagDatabase extracted.");
        }
    }

    private async Task ExtractP4kXmlFiles(string p4kFile, IConsole console)
    {
        var p4k = P4kFile.FromFile(p4kFile);
        var outputDir = Path.Combine(OutputDirectory, "P4kContents");
        Directory.CreateDirectory(outputDir);

        var mainXmlEntries = p4k.Entries
            .Where(e => e.Name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var socpakEntries = p4k.Entries
            .Where(e => (e.Name.EndsWith(".socpak", StringComparison.OrdinalIgnoreCase) || 
                         e.Name.EndsWith(".pak", StringComparison.OrdinalIgnoreCase)) &&
                        !e.Name.Contains("shadercache_", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var socpakXmlEntries = new List<(P4kEntry entry, string socpakPath, P4kFile socpak)>();
        foreach (var socpakEntry in socpakEntries)
        {
            try
            {
                var socpak = P4kFile.FromP4kEntry(p4k, socpakEntry);
                var xmlsInSocpak = socpak.Entries
                    .Where(e => e.Name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                    .Select(e => (entry: e, socpakPath: socpakEntry.RelativeOutputPath, socpak: socpak))
                    .ToList();
                
                socpakXmlEntries.AddRange(xmlsInSocpak);
            }
            catch
            {
                // Skip problematic SOCPAK files
            }
        }

        var totalFiles = mainXmlEntries.Count + socpakXmlEntries.Count;
        
        if (totalFiles == 0)
        {
            await console.Output.WriteLineAsync("No XML files found in P4K or SOCPAKs.");
            return;
        }

        foreach (var entry in mainXmlEntries)
        {
            ExtractXmlEntry(p4k, entry, outputDir, entry.RelativeOutputPath);
        }

        foreach (var (entry, socpakPath, socpak) in socpakXmlEntries)
        {
            var socpakDir = Path.GetDirectoryName(socpakPath) ?? "";
            var socpakName = Path.GetFileNameWithoutExtension(socpakPath);
            var fullOutputPath = Path.Combine(socpakDir, socpakName, entry.RelativeOutputPath);
            
            ExtractXmlEntry(socpak, entry, outputDir, fullOutputPath);
        }

        await console.Output.WriteLineAsync($"Extracted {mainXmlEntries.Count} XML files from P4K and {socpakXmlEntries.Count} from SOCPAKs.");
    }

    private void ExtractXmlEntry(P4kFile p4k, P4kEntry entry, string baseOutputDir, string relativePath)
    {
        using var entryStream = p4k.OpenStream(entry);
        var ms = new MemoryStream();
        entryStream.CopyTo(ms);
        ms.Position = 0;

        var entryPath = Path.Combine(baseOutputDir, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(entryPath)!);

        if (CryXml.IsCryXmlB(ms))
        {
            ms.Position = 0;
            if (CryXml.TryOpen(ms, out var cryXml))
            {
                File.WriteAllText(entryPath, cryXml.ToString());
            }
            else
            {
                using var fs = File.Create(entryPath);
                ms.Position = 0;
                ms.CopyTo(fs);
            }
        }
        else
        {
            using var fs = File.Create(entryPath);
            ms.Position = 0;
            ms.CopyTo(fs);
        }
    }

    private static async Task ExtractDataCoreIntoZip(string p4kFile, string zipPath)
    {
        var p4k = new P4kFileSystem(P4kFile.FromFile(p4kFile));
        Stream? input = null;
        foreach (var file in DataCoreUtils.KnownPaths)
        {
            if (!p4k.FileExists(file)) continue;
            input = p4k.OpenRead(file);
            break;
        }

        if (input == null)
            throw new InvalidOperationException("DataCore not found.");

        Directory.CreateDirectory(Path.GetDirectoryName(zipPath)!);
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