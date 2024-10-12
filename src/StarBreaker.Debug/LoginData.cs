using System.Text.Json.Serialization;

namespace StarBreaker.Debug;

public record StarNetwork
{
    [JsonPropertyName("services_endpoint")]
    public string ServicesEndpoint { get; init; } = null!;

    [JsonPropertyName("hostname")] public string Hostname { get; init; } = null!;

    [JsonPropertyName("port")] public int Port { get; init; }
}

public record LoginData
{
    [JsonPropertyName("username")] public string Username { get; init; } = null!;

    [JsonPropertyName("token")] public string Token { get; init; } = null!;

    [JsonPropertyName("auth_token")] public string AuthToken { get; init; } = null!;
    
    [JsonPropertyName("star_network")] public StarNetwork StarNetwork { get; init; } = null!;
}