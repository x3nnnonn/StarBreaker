using System.Text.Json.Serialization;
using StarBreaker.Chf;

namespace StarBreaker.Cli;

[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(DownloadCommand.SccRoot))]
[JsonSerializable(typeof(DownloadCommand.SccBody))]
[JsonSerializable(typeof(DownloadCommand.SccCharacter))]
[JsonSerializable(typeof(ChfData))]
internal partial class CliSerializerContext : JsonSerializerContext;