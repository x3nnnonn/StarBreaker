using System.Text.Json.Serialization;
using StarBreaker.Chf;

namespace StarBreaker.Chf;

[JsonSerializable(typeof(ChfData))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal partial class ChfSerializerContext : JsonSerializerContext;