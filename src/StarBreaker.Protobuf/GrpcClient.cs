using System.Text.Json;
using System.Text.Json.Serialization;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using Sc.External.Services.CharacterCustomizer.V1;
using Sc.External.Services.Entitygraph.V1;
using StarBreaker.Grpc;

namespace StarBreaker.Sandbox;

/// <summary>
/// This is here mostly so I can stop referencing the grpc project in actually useful ones.
///  It takes ages to compile and I don't want to do it every time I make a change to the protos.
/// </summary>
public static class GrpcClient
{
    public static async Task RunAsync()
    {
        const string testEntity = @"C:\Users\Diogo\Downloads\EntityQuery.grpc";
        const string testChar = @"C:\Users\Diogo\Downloads\everything.grpc";
        
        var enity = EntityQueryResponse.Parser.ParseFrom(GrpcUtils.GrpcToProtobuf(File.ReadAllBytes(testEntity)));
        return;

        var bytes = File.ReadAllBytes(testChar);
        var character = SaveCharacterCustomizationsRequest.Parser.ParseFrom(GrpcUtils.GrpcToProtobuf(bytes));

        var json = character.ToString();
        Console.WriteLine(json);
        return;
        
        var scWatcher = new StarCitizenClientWatcher(@"C:\Program Files\Roberts Space Industries\StarCitizen\");
        scWatcher.Start();
        var loginData = await scWatcher.WaitForLoginData();

        Console.WriteLine($"Got Login data for user: \"{loginData.Username}\" on server: \"{loginData.StarNetwork.ServicesEndpoint}\"");

        var creds = CallCredentials.FromInterceptor((_, metadata) =>
        {
            metadata.Add("Authorization", $"Bearer {loginData.AuthToken}");

            return Task.CompletedTask;
        });

        var channel = GrpcChannel.ForAddress(new Uri(loginData.StarNetwork.ServicesEndpoint), new GrpcChannelOptions
        {
            Credentials = ChannelCredentials.Create(ChannelCredentials.SecureSsl, creds)
        });
        var charClient = new CharacterCustomizerService.CharacterCustomizerServiceClient(channel);

        ChangeDna(character);
        ChangeEyeColor(character);
        ChangeBodyColor(character);

        var response = charClient.SaveCharacterCustomizations(character);

        Console.WriteLine(response);
        return;
    }
    
    private static void ChangeDna(SaveCharacterCustomizationsRequest req)
    {
        req.CharacterCustomizations.DnaMatrix = ByteString.CopyFrom(Convert.FromHexString(
            "9493d0fc54ebf49e0452d765000000000c00040004000000feff1700feff1700feff1700feff1700feff1700feff1700feff1700feff1700feff1700feff1700feff1700feff1700000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000"));
    }

    private static void ChangeEyeColor(SaveCharacterCustomizationsRequest saveCharacterCustomizationsRequest)
    {
        ChangeColorWithId(saveCharacterCustomizationsRequest, 0x442a34ac, 214, 30, 30);
    }

    private static void ChangeBodyColor(SaveCharacterCustomizationsRequest saveCharacterCustomizationsRequest)
    {
        //only changes head for now, todo
        ChangeColorWithId(saveCharacterCustomizationsRequest, 0xbd530797, 30, 30, 200);
    }

    private static void ChangeColorWithId(SaveCharacterCustomizationsRequest req, uint id, byte r, byte g, byte b)
    {
        var materialParams = req.CharacterCustomizations.CustomMaterialParams;
        if (materialParams == null)
            return;

        var copy = new byte[materialParams.Length];
        materialParams.CopyTo(copy, 0);
        var span = copy.AsSpan();
        ReadOnlySpan<byte> colorKey = BitConverter.GetBytes(id);
    
        var colorIndex = span.IndexOf(colorKey);
        if (colorIndex == -1)
            return;
    
        var colorLocation = colorIndex + colorKey.Length;
        span[colorLocation + 0] = r; //R
        span[colorLocation + 1] = g; //G
        span[colorLocation + 2] = b; //B
        span[colorLocation + 3] = 0xFF; //A
    
        req.CharacterCustomizations.CustomMaterialParams = ByteString.CopyFrom(copy);
    }
}

public class StarCitizenClientWatcher : IDisposable
{
    private readonly FileSystemWatcher _watcher;

    public event EventHandler<LoginData>? LoginDataChanged;

    public StarCitizenClientWatcher(string starCitizenBaseFolder)
    {
        _watcher = new FileSystemWatcher
        {
            Path = starCitizenBaseFolder,
            IncludeSubdirectories = true,
            Filter = "loginData.json",
            NotifyFilter = NotifyFilters.LastWrite
        };
        _watcher.Changed += OnChanged;
    }

    public void Start()
    {
        _watcher.EnableRaisingEvents = true;
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        var loginData = JsonSerializer.Deserialize(File.ReadAllText(e.FullPath), JsonContext.Default.LoginData);

        if (loginData == null)
            return;

        LoginDataChanged?.Invoke(this, loginData);
    }

    public async Task<LoginData> WaitForLoginData()
    {
        var targetFile = Directory.GetFiles(_watcher.Path, "loginData.json", SearchOption.AllDirectories).FirstOrDefault();
        if (targetFile != null && File.Exists(targetFile))
        {
            try
            {
                var loginData = JsonSerializer.Deserialize(await File.ReadAllTextAsync(targetFile), JsonContext.Default.LoginData);
                if (loginData != null)
                    return loginData;
            }
            catch
            {
                //this is fine, the file is probably empty or something
            }
        }

        var tcs = new TaskCompletionSource<LoginData>();
        LoginDataChanged += (_, data) => tcs.SetResult(data);
        return await tcs.Task;
    }

    public void Dispose()
    {
        _watcher.EnableRaisingEvents = false;
        _watcher.Changed -= OnChanged;
        _watcher.Dispose();
    }
}

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

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(LoginData))]
[JsonSerializable(typeof(StarNetwork))]
public partial class JsonContext : JsonSerializerContext;