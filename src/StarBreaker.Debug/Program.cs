using System.Text.Json;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using Sc.External.Services.CharacterCustomizer.V1;

var env = JsonSerializer.Deserialize<Environment>(File.ReadAllText("env.json"));
if (env == null || string.IsNullOrWhiteSpace(env.Token) || string.IsNullOrWhiteSpace(env.Url))
{
    Console.WriteLine("Failed to read env.json");
    return;
}

var creds = CallCredentials.FromInterceptor((_, metadata) =>
{
    metadata.Add("Authorization", $"Bearer {env.Token}");

    return Task.CompletedTask;
});

var channel = GrpcChannel.ForAddress(env.Url, new GrpcChannelOptions
{
    Credentials = ChannelCredentials.Create(ChannelCredentials.SecureSsl, creds)
});
var charClient = new CharacterCustomizerService.CharacterCustomizerServiceClient(channel);

const string testChar = @"C:\Users\Diogo\Downloads\default_f_diff_eyecolor.grpc";

var bytes = GrpcToProtoBytes(File.ReadAllBytes(testChar));
var character = SaveCharacterCustomizationsRequest.Parser.ParseFrom(bytes);

ChangeDna(character);
ChangeEyeColor(character);
ChangeBodyColor(character);

var response = charClient.SaveCharacterCustomizations(character);

Console.WriteLine(response);
return;

//TODO: make this not stupid
byte[] GrpcToProtoBytes(byte[] proto)
{
    return proto[5..];
}

void ChangeDna(SaveCharacterCustomizationsRequest req)
{
    req.CharacterCustomizations.DnaMatrix = ByteString.CopyFrom(Convert.FromHexString(
        "9493d0fc54ebf49e0452d765000000000c00040004000000feff1700feff1700feff1700feff1700feff1700feff1700feff1700feff1700feff1700feff1700feff1700feff1700000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000"));
}

void ChangeEyeColor(SaveCharacterCustomizationsRequest saveCharacterCustomizationsRequest)
{
    ChangeColorWithId(saveCharacterCustomizationsRequest, 0x442a34ac, 214, 30, 30);
}

void ChangeBodyColor(SaveCharacterCustomizationsRequest saveCharacterCustomizationsRequest)
{
    //only changes head for now, todo
    ChangeColorWithId(saveCharacterCustomizationsRequest, 0xbd530797, 30, 30, 200);
}

void ChangeColorWithId(SaveCharacterCustomizationsRequest req, uint id, byte r, byte g, byte b)
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