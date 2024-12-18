using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using StarBreaker.Protobuf;

namespace StarBreaker.Chf;

[Command("proto-set-extract", Description = "Extracts the protobuf descriptor set from the Star Citizen executable.")]
public class ExtractDescriptorSetCommand : ICommand
{
    [CommandOption("input", 'i', Description = "The path to the Star Citizen executable.")]
    public required string Input { get; init; } = string.Empty;

    [CommandOption("output", 'o', Description = "The path to the output file.")]
    public required string Output { get; init; } = string.Empty;

    public ValueTask ExecuteAsync(IConsole console)
    {
        console.Output.WriteLine("Extracting protobuf descriptor set...");
        var extractor = ProtobufExtractor.FromFilename(Input);
        extractor.WriteDescriptorSet(Output);
        console.Output.WriteLine("Wrote descriptor set to {0}", Output);
        
        return default;
    }
}