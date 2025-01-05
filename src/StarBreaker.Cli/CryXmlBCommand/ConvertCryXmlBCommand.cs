using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using StarBreaker.CryXmlB;

namespace StarBreaker.Cli;

[Command("cryxml-convert", Description = "Converts a CryXmlB file to a XML file")]
public sealed class ConvertCryXmlBCommand : ICommand
{
    [CommandOption("input", 'i', Description = "Input CryXmlB file")]
    public required string? Input { get; set; }

    [CommandOption("output", 'o', Description = "Output XML file")]
    public required string? Output { get; set; }

    public ValueTask ExecuteAsync(IConsole console)
    {
        if (string.IsNullOrWhiteSpace(Input))
        {
            console.Error.WriteLine("Input file is required");
            return default;
        }

        if (string.IsNullOrWhiteSpace(Output))
        {
            console.Error.WriteLine("Output file is required");
            return default;
        }

        if (!File.Exists(Input))
        {
            console.Error.WriteLine("Input file not found");
            return default;
        }

        var outFileDir = Path.GetDirectoryName(Output);
        if (!string.IsNullOrWhiteSpace(outFileDir) && !Directory.Exists(outFileDir))
            Directory.CreateDirectory(outFileDir);

        if (!CryXml.TryOpen(File.OpenRead(Input), out var cryXml))
        {
            console.Error.WriteLine("Invalid CryXmlB file");
            return default;
        }

        cryXml.Save(Output);

        return default;
    }
}