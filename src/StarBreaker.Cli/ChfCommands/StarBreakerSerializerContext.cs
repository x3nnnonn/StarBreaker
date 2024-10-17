using System.Text.Json.Serialization;

namespace StarBreaker.Chf;

[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(DownloadCommand.SccRoot))]
[JsonSerializable(typeof(DownloadCommand.SccBody))]
[JsonSerializable(typeof(DownloadCommand.SccCharacter))]
[JsonSerializable(typeof(StarCitizenCharacter))]
internal partial class StarBreakerSerializerContext : JsonSerializerContext;