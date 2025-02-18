using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

// ReSharper disable UnassignedGetOnlyAutoProperty
namespace StarBreaker.Common;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
[JsonConverter(typeof(ColorRgbaJsonConverter))]
public readonly struct ColorRgba(byte r, byte g, byte b)
{
    public byte R { get; } = r;
    public byte G { get; } = g;
    public byte B { get; } = b;

    //Alpha seems to be unused. Keep it for alignment.
    private readonly byte _A;
    
    public override string ToString() => $"#{R:X2}{G:X2}{B:X2}";
}

public class ColorRgbaJsonConverter : JsonConverter<ColorRgba>
{
    public override ColorRgba Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var hex = reader.GetString();
        if (hex is not { Length: 7 } || hex[0] != '#')
            throw new JsonException("Invalid color format");

        return new ColorRgba(
            byte.Parse(hex[1..3], NumberStyles.HexNumber),
            byte.Parse(hex[3..5], NumberStyles.HexNumber),
            byte.Parse(hex[5..7], NumberStyles.HexNumber)
        );
    }

    public override void Write(Utf8JsonWriter writer, ColorRgba value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}