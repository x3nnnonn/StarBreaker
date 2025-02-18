using System.Text.Json;
using StarBreaker.Chf;
using StarBreaker.Common;

namespace StarBreaker.Chf;

public static class ChfProcessing
{
    public static async Task ProcessCharacter(string chf)
    {
        if (!chf.EndsWith(".chf"))
            throw new ArgumentException("Not a chf file", nameof(chf));

        var bin = Path.ChangeExtension(chf, ".bin");
        var chfFile = ChfFile.FromChf(chf);
        await chfFile.WriteToBinFileAsync(bin);
        
        var json = Path.ChangeExtension(chf, ".json");
        var data = await File.ReadAllBytesAsync(bin);
        var character = ChfData.FromBytes(data);
        
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            TypeInfoResolver = ChfSerializerContext.Default,
            Converters = { new ColorRgbaJsonConverter() },
        };
        var jsonString = JsonSerializer.Serialize(character, options);
        await File.WriteAllTextAsync(json, jsonString);
    }
}