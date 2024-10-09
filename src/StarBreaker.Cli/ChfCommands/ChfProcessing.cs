using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using StarBreaker.Chf;

namespace StarBreaker.Cli;

public static class ChfProcessing
{
    private static readonly JsonSerializerOptions opts = new() { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };

    public static async Task ProcessCharacter(string chf)
    {
        if (!chf.EndsWith(".chf"))
            throw new ArgumentException("Not a chf file", nameof(chf));

        var bin = Path.ChangeExtension(chf, ".bin");
        var chfFile = ChfFile.FromChf(chf);
        await chfFile.WriteToBinFileAsync(bin);
        
        var json = Path.ChangeExtension(chf, ".json");
        var data = await File.ReadAllBytesAsync(bin);
        var character = StarCitizenCharacter.FromBytes(data);
        var jsonString = JsonSerializer.Serialize(character, opts);
        await File.WriteAllTextAsync(json, jsonString);
    }
}