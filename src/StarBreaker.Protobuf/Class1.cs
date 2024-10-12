using Grpc.Net.Client;
using Sc.External.Services.CharacterCustomizer.V1;
using Sc.External.Services.Chat.V1;

namespace StarBreaker.Protobuf;

public class Class1
{
    public void Test()
    {
        var channel = GrpcChannel.ForAddress("");
        var client = new CharacterCustomizerService.CharacterCustomizerServiceClient(channel);

        //etc
        client.SaveCharacterCustomizations(new SaveCharacterCustomizationsRequest()
        {
        });
    }
}