using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using StarBreaker.Protobuf;

namespace StarBreaker.Chf;

[Command("proto-extract", Description = "Extracts protobuf definitions from the Star Citizen executable.")]
public class ExtractProtobufsCommand : ICommand
{
    [CommandOption("input", 'i', Description = "The path to the Star Citizen executable.")]
    public required string Input { get; init; } = string.Empty;

    [CommandOption("output", 'o', Description = "The path to the output directory.")]
    public required string Output { get; init; } = string.Empty;

    public ValueTask ExecuteAsync(IConsole console)
    {
        console.Output.WriteLine("Extracting protobuf definitions...");
        var extractor = new ProtobufExtractor(Input);
        extractor.WriteProtos(Output, p => !p.Name.StartsWith("google/protobuf"));
        console.Output.WriteLine("Wrote {0} protobuf definitions to {1}", extractor.FileDescriptorProtos.Count, Output);
        
        return default;
    }
}