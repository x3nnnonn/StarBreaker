using System.Text.Json.Serialization;
using StarBreaker.Chf.Parser;

namespace StarBreaker.Chf;

[JsonSerializable(typeof(StarCitizenCharacter))]
[JsonSerializable(typeof(ChfDataParser))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal partial class ChfSerializerContext : JsonSerializerContext;