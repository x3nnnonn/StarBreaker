using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using StarBreaker.Extraction;

namespace StarBreaker.Cli;

[Command("p4k-dump", Description = "Dumps the contents a Game.p4k file")]
public class DumpP4kCommand : ICommand
{
    [CommandOption("p4k", 'p', Description = "Path to the Game.p4k", EnvironmentVariable = "INPUT_P4K")]
    public required string P4kFile { get; init; }

    [CommandOption("output", 'o', Description = "Path to the output directory", EnvironmentVariable = "OUTPUT_FOLDER")]
    public required string OutputDirectory { get; init; }

    [CommandOption("text-format", 't', Description = "Output text format", EnvironmentVariable = "TEXT_FORMAT")]
    public string? TextFormat { get; init; }

    public ValueTask ExecuteAsync(IConsole console)
    {
        var p4k = P4kDirectoryNode.FromP4k(P4k.P4kFile.FromFile(P4kFile));

        var useJson = TextFormat switch
        {
            "json" => true,
            _ => false,
        };

        WriteFileForNode(OutputDirectory, p4k, useJson);

        return default;
    }

    private static void WriteFileForNode(string baseDir, P4kDirectoryNode directoryNode, bool useJson)
    {
        //init both because i'm too lazy to refactor the code to only init when needed
        var dir = new XElement("Directory",
            new XAttribute("Name", directoryNode.Name)
        );

        var jsonDir = new JsonObject { { "Name", directoryNode.Name } };
        var jsonChildren = new JsonArray();
        jsonDir.Add("Files", jsonChildren);

        foreach (var (_, childNode) in directoryNode.Children.OrderBy(x => x.Key))
        {
            switch (childNode)
            {
                case P4kDirectoryNode childDirectoryNode:
                    //if we're a directory, Call ourselves recursively
                    WriteFileForNode(Path.Combine(baseDir, childDirectoryNode.Name), childDirectoryNode, useJson);
                    break;
                case P4kFileNode childFileNode:
                    if (useJson)
                    {
                        jsonChildren.Add(new JsonObject
                        {
                            ["Name"] = Path.GetFileName(childFileNode.P4KEntry.Name),
                            ["CRC32"] = $"0x{childFileNode.P4KEntry.Crc32:X8}",
                            ["Size"] = childFileNode.P4KEntry.UncompressedSize.ToString(CultureInfo.InvariantCulture),
                            ["CompressionType"] = childFileNode.P4KEntry.CompressionMethod.ToString(CultureInfo.InvariantCulture),
                            ["Encrypted"] = childFileNode.P4KEntry.IsCrypted.ToString(CultureInfo.InvariantCulture)
                        });
                    }
                    else
                    {
                        dir.Add(new XElement("File",
                            new XAttribute("Name", Path.GetFileName(childFileNode.P4KEntry.Name)),
                            new XAttribute("CRC32", $"0x{childFileNode.P4KEntry.Crc32:X8}"),
                            //Revisit: they seem to change lastmodified a lot while the crc32 stays the same. I'll just ignore the date for now.
                            // new XAttribute("LastModified", childFileNode.ZipEntry.LastModified.ToString("O")),
                            new XAttribute("Size", childFileNode.P4KEntry.UncompressedSize.ToString(CultureInfo.InvariantCulture)),
                            //new XAttribute("CompressedSize", childFileNode.ZipEntry.CompressedSize.ToString(CultureInfo.InvariantCulture)),
                            new XAttribute("CompressionType", childFileNode.P4KEntry.CompressionMethod.ToString(CultureInfo.InvariantCulture)),
                            new XAttribute("Encrypted", childFileNode.P4KEntry.IsCrypted.ToString(CultureInfo.InvariantCulture))
                        ));
                    }

                    break;
                default:
                    throw new InvalidOperationException("Unknown node type");
            }
        }

        if (useJson)
        {
            if (jsonChildren.Count > 0)
            {
                var filePath = Path.Combine(baseDir, directoryNode.Name) + ".json";
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

                using var fs = File.OpenWrite(filePath);
                using var writer = new Utf8JsonWriter(fs, new JsonWriterOptions { Indented = true });
                jsonDir.WriteTo(writer);
            }
        }
        else
        {
            if (dir.HasElements)
            {
                var filePath = Path.Combine(baseDir, directoryNode.Name) + ".xml";
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                dir.Save(filePath);
            }
        }
    }
}