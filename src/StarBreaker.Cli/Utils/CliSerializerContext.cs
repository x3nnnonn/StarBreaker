using System.Text.Json.Serialization;

namespace StarBreaker.Cli;

[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(DownloadCommand.SccRoot))]
[JsonSerializable(typeof(DownloadCommand.SccBody))]
[JsonSerializable(typeof(DownloadCommand.SccCharacter))]
internal partial class CliSerializerContext : JsonSerializerContext;