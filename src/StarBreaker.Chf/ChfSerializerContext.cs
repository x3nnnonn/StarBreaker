using System.Text.Json.Serialization;

namespace StarBreaker.Chf;

[JsonSerializable(typeof(StarCitizenCharacter))]
internal partial class ChfSerializerContext : JsonSerializerContext;