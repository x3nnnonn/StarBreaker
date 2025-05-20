using System.Runtime.InteropServices;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using Sc.External.Services.CharacterCustomizer.V1;
using StarBreaker.Common;

namespace StarBreaker.Sandbox;

public static class IllegalCharacterCustomization
{
    public static void Run(GrpcChannel channel, Metadata authHeaders)
    {
        const string characterJson = @"C:\Development\StarCitizen\StarBreaker\src\StarBreaker.GrpcClient\female.json";

        var headmat = new CigGuid("e186048a-9a81-47b3-828e-71e957c65762");
        var headMatBytes = MemoryMarshal.Cast<CigGuid, byte>([headmat]);

        var character = SaveCharacterCustomizationsRequest.Parser.ParseJson(File.ReadAllText(characterJson));
        var bytes = character.CharacterCustomizations.CustomMaterialParams.ToArray();

        //var newMat = new CigGuid("ede6e28a-1f44-402b-8b8f-8eb5174f887f");
        var newMat = new CigGuid("6593ef6e-f7e1-4369-a9fc-ba79883b5413");

        var newMatBytes = MemoryMarshal.Cast<CigGuid, byte>([newMat]);

        //female body COLOR
        ReplaceAll(bytes, [0xfe, 0x33, 0x00, 0xff], [0xff, 0xff, 0xff, 0xff]);

        //male body COLOR
        //ReplaceAll(bytes, [0x51, 0x34, 0x28, 0xff], [0xff,0xff,0xff,0xff]);

        ReplaceAll(bytes, headMatBytes, newMatBytes);

        character.CharacterCustomizations.CustomMaterialParams = ByteString.CopyFrom(bytes);


        var charClient = new CharacterCustomizerService.CharacterCustomizerServiceClient(channel);

        //ChangeDna(character);
        //ChangeEyeColor(character);
        //ChangeBodyColor(character);


        var response = charClient.SaveCharacterCustomizations(character, authHeaders);

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

    private static void ReplaceAll(Span<byte> span, ReadOnlySpan<byte> needle, ReadOnlySpan<byte> replacement)
    {
        var idx = span.IndexOf(needle);
        while (idx != -1)
        {
            Console.WriteLine($"Found needle at {idx}");
            replacement.CopyTo(span[idx..]);
            idx = span.IndexOf(needle);
        }
    }
}