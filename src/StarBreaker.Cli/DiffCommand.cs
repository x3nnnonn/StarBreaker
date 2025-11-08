using System.Diagnostics;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using StarBreaker.Common;
using StarBreaker.CryChunkFile;
using StarBreaker.CryXmlB;
using StarBreaker.DataCore;
using StarBreaker.Dds;
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

    [CommandOption("extract-dds", Description = "Extract DDS files as PNG", EnvironmentVariable = "EXTRACT_DDS")]
    public bool ExtractDds { get; init; }

    [CommandOption("diff-against", Description = "Path to previous P4K file or output directory to compare against for extracting only new/modified DDS files", EnvironmentVariable = "DIFF_AGAINST")]
    public string? DiffAgainst { get; init; }

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
                Path.Combine(OutputDirectory, "DDS_Files"),
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

        if (ExtractDds)
        {
            await ExtractDdsFiles(p4kFile, console, DiffAgainst);
            await console.Output.WriteLineAsync("DDS files extracted in " + sw.Elapsed);
            sw.Restart();
        }

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

        var mainSocEntries = p4k.Entries
            .Where(e => e.Name.EndsWith(".soc", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var socpakEntries = p4k.Entries
            .Where(e => (e.Name.EndsWith(".socpak", StringComparison.OrdinalIgnoreCase) || 
                         e.Name.EndsWith(".pak", StringComparison.OrdinalIgnoreCase)) &&
                        !e.Name.Contains("shadercache_", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var socpakXmlEntries = new List<(P4kEntry entry, string socpakPath, P4kFile socpak)>();
        var socpakSocEntries = new List<(P4kEntry entry, string socpakPath, P4kFile socpak)>();
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

                var socsInSocpak = socpak.Entries
                    .Where(e => e.Name.EndsWith(".soc", StringComparison.OrdinalIgnoreCase))
                    .Select(e => (entry: e, socpakPath: socpakEntry.RelativeOutputPath, socpak: socpak))
                    .ToList();

                socpakSocEntries.AddRange(socsInSocpak);
            }
            catch
            {
                // Skip problematic SOCPAK files
            }
        }

        var totalXmlFiles = mainXmlEntries.Count + socpakXmlEntries.Count;
        var totalSocFiles = mainSocEntries.Count + socpakSocEntries.Count;
        
        if (totalXmlFiles == 0 && totalSocFiles == 0)
        {
            await console.Output.WriteLineAsync("No XML or SOC files found in P4K or SOCPAKs.");
            return;
        }

        foreach (var entry in mainXmlEntries)
        {
            ExtractXmlEntry(p4k, entry, outputDir, entry.RelativeOutputPath);
        }

        foreach (var entry in mainSocEntries)
        {
            ExtractSocEntry(p4k, entry, outputDir, entry.RelativeOutputPath);
        }

        foreach (var (entry, socpakPath, socpak) in socpakXmlEntries)
        {
            var socpakDir = Path.GetDirectoryName(socpakPath) ?? "";
            var socpakName = Path.GetFileNameWithoutExtension(socpakPath);
            var fullOutputPath = Path.Combine(socpakDir, socpakName, entry.RelativeOutputPath);
            
            ExtractXmlEntry(socpak, entry, outputDir, fullOutputPath);
        }

        foreach (var (entry, socpakPath, socpak) in socpakSocEntries)
        {
            var socpakDir = Path.GetDirectoryName(socpakPath) ?? "";
            var socpakName = Path.GetFileNameWithoutExtension(socpakPath);
            var fullOutputPath = Path.Combine(socpakDir, socpakName, entry.RelativeOutputPath);

            ExtractSocEntry(socpak, entry, outputDir, fullOutputPath);
        }

        await console.Output.WriteLineAsync($"Extracted {mainXmlEntries.Count} XML files from P4K and {socpakXmlEntries.Count} from SOCPAKs.");
        await console.Output.WriteLineAsync($"Extracted {mainSocEntries.Count} SOC files from P4K and {socpakSocEntries.Count} from SOCPAKs.");
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

    private void ExtractSocEntry(P4kFile p4k, P4kEntry entry, string baseOutputDir, string relativePath)
    {
        using var entryStream = p4k.OpenStream(entry);
        using var ms = new MemoryStream();
        entryStream.CopyTo(ms);
        var socBytes = ms.ToArray();

        var adjustedRelativePath = NormalizeSocRelativePath(relativePath);
        var entryPath = Path.Combine(baseOutputDir, adjustedRelativePath);
        var objectContainerDir = Path.GetDirectoryName(entryPath) ?? baseOutputDir;
        var baseName = Path.GetFileNameWithoutExtension(entryPath);
        Directory.CreateDirectory(objectContainerDir);

        try
        {
            if (!CrChFile.TryRead(socBytes, out var socFile))
            {
                var rawPath = Path.Combine(objectContainerDir, Path.GetFileName(entryPath));
                File.WriteAllBytes(rawPath, socBytes);
                return;
            }

            var xmlChunks = 0;
            for (int i = 0; i < socFile.Chunks.Length; i++)
            {
                var chunk = socFile.Chunks[i];
                if (!CryXml.IsCryXmlB(chunk))
                    continue;

                using var chunkStream = new MemoryStream(chunk);
                var cryXml = new CryXml(chunkStream);
                var xmlPath = Path.Combine(objectContainerDir, $"{baseName}_{i}.xml");
                File.WriteAllText(xmlPath, cryXml.ToString());
                xmlChunks++;
            }

            if (xmlChunks == 0)
            {
                var rawPath = Path.Combine(objectContainerDir, Path.GetFileName(entryPath));
                File.WriteAllBytes(rawPath, socBytes);
            }
        }
        catch
        {
            var fallbackPath = Path.Combine(objectContainerDir, Path.GetFileName(entryPath));
            if (!File.Exists(fallbackPath))
            {
                File.WriteAllBytes(fallbackPath, socBytes);
            }
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

    private async Task ExtractDdsFiles(string p4kFile, IConsole console, string? diffAgainst)
    {
        try
        {
            var p4k = P4kFile.FromFile(p4kFile);
            var outputDir = Path.Combine(OutputDirectory, "DDS_Files");
            Directory.CreateDirectory(outputDir);

            var p4kFileSystem = new P4kFileSystem(p4k);

            IEnumerable<P4kEntry> ddsEntriesToExtract;

            if (!string.IsNullOrWhiteSpace(diffAgainst))
            {
                // Extract only new/modified DDS files by comparing with previous version
                var previousP4kPath = GetPreviousP4kPath(diffAgainst);
                if (File.Exists(previousP4kPath))
                {
                    var previousP4k = P4kFile.FromFile(previousP4kPath);
                    var comparisonRoot = P4kComparison.Compare(previousP4k, p4k);
                    
                    var allFiles = comparisonRoot.GetAllFiles().ToList();
                    var ddsFiles = allFiles
                        .Where(f => f.Status == P4kComparisonStatus.Added || f.Status == P4kComparisonStatus.Modified)
                        .Where(f => Path.GetFileName(f.FullPath).Contains(".dds", StringComparison.OrdinalIgnoreCase))
                        .Where(f => f.RightEntry != null)
                        .Where(f => !char.IsDigit(Path.GetFileName(f.FullPath)[^1])) // Filter out mipmap files
                        .Select(f => f.RightEntry!)
                        .ToList();
                    
                    ddsEntriesToExtract = ddsFiles;
                    await console.Output.WriteLineAsync($"Found {ddsFiles.Count} new/modified DDS files to extract.");
                }
                else
                {
                    await console.Output.WriteLineAsync($"Previous P4K not found at {previousP4kPath}. Extracting all DDS files.");
                    ddsEntriesToExtract = GetBaseDdsEntries(p4k);
                }
            }
            else
            {
                // Extract all DDS files if no comparison specified
                ddsEntriesToExtract = GetBaseDdsEntries(p4k);
            }

            var processedCount = 0;
            var failedCount = 0;

            foreach (var entry in ddsEntriesToExtract)
            {
                try
                {
                    var fileName = Path.GetFileName(entry.Name);
                    var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                    
                    if (fileNameWithoutExt.EndsWith(".dds", StringComparison.OrdinalIgnoreCase))
                    {
                        fileNameWithoutExt = fileNameWithoutExt.Substring(0, fileNameWithoutExt.Length - 4);
                    }
                    
                    var pngOutputPath = Path.Combine(outputDir, fileNameWithoutExt + ".png");

                    using var ms = DdsFile.MergeToStream(entry.Name, p4kFileSystem);
                    var ddsBytes = ms.ToArray();
                    using var pngStream = DdsFile.ConvertToPng(ddsBytes, true, true);
                    
                    var pngBytes = pngStream.ToArray();
                    File.WriteAllBytes(pngOutputPath, pngBytes);
                    processedCount++;
                }
                catch (Exception ex)
                {
                    failedCount++;
                    console.Output.WriteLine($"Failed to extract DDS: {entry.Name} - {ex.Message}");
                }
            }

            await console.Output.WriteLineAsync($"Extracted {processedCount} DDS files ({failedCount} failed).");
        }
        catch (Exception ex)
        {
            await console.Output.WriteLineAsync($"Error extracting DDS files: {ex.Message}");
        }
    }

    private List<P4kEntry> GetBaseDdsEntries(P4kFile p4k)
    {
        return p4k.Entries
            .Where(e => e.Name.EndsWith(".dds", StringComparison.OrdinalIgnoreCase) || 
                       e.Name.EndsWith(".dds.a", StringComparison.OrdinalIgnoreCase))
            .Where(e => !e.Name.EndsWith(".ddna.dds", StringComparison.OrdinalIgnoreCase) &&
                       !e.Name.EndsWith(".ddna.dds.n", StringComparison.OrdinalIgnoreCase))
            .Where(e => !char.IsDigit(e.Name[^1]))
            .ToList();
    }

    private string GetPreviousP4kPath(string diffAgainst)
    {
        // If it's a file path, use it directly
        if (File.Exists(diffAgainst))
        {
            return diffAgainst;
        }
        
        // If it's a directory, check if it contains a previous diff output
        if (Directory.Exists(diffAgainst))
        {
            // First check for a .p4k file in the directory
            var p4kFile = Directory.GetFiles(diffAgainst, "*.p4k", SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (p4kFile != null)
            {
                return p4kFile;
            }
            
            // Try to find P4k dump folder with the old structure
            var p4kDumpDir = Path.Combine(diffAgainst, "P4k");
            if (Directory.Exists(p4kDumpDir))
            {
                // Look for the latest dump file in the P4k directory
                var latestDump = Directory.GetFiles(p4kDumpDir, "*.p4k", SearchOption.AllDirectories)
                    .OrderByDescending(f => File.GetLastWriteTime(f))
                    .FirstOrDefault();
                if (latestDump != null)
                {
                    return latestDump;
                }
            }
        }
        
        return diffAgainst;
    }

    private static string NormalizeSocRelativePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return relativePath;

        var trimmed = relativePath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Replace('\\', '/');

        var parts = trimmed.Split('/', StringSplitOptions.RemoveEmptyEntries).ToList();

        if (parts.Count > 0 && parts[0].Equals("ObjectContainers", StringComparison.OrdinalIgnoreCase))
        {
            parts.RemoveAt(0);
        }

        if (parts.Count > 0 && parts[0].Equals("Data", StringComparison.OrdinalIgnoreCase) == false)
        {
            parts.Insert(0, "Data");
        }

        var normalizedPath = Path.Combine(parts.ToArray());
        return normalizedPath;
    }
}