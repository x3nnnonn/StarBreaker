using System.Text.Json.Serialization;

namespace StarBreaker.Sandbox;

public record StarNetwork
{
    [JsonPropertyName("services_endpoint")]
    public string ServicesEndpoint { get; init; } = null!;

    [JsonPropertyName("hostname")] public string Hostname { get; init; } = null!;

    [JsonPropertyName("port")] public int Port { get; init; }
}