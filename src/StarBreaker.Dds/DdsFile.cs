using System.Runtime.InteropServices;
using Pfim;
using SkiaSharp;

namespace StarBreaker.Dds;

public class DdsFile
{
    public static DdsFile FromCombinedBytes(byte[] buffer)
    {
        using var image = Pfimage.FromStream(new MemoryStream(buffer));


        return null;
    }

    public static DdsFile FromFile(string fullPath)
    {
        if (!fullPath.EndsWith(".dds", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("File must be a DDS file");

        var files = new List<string>();
        files.Add(fullPath);

        var containingFolder = Path.GetDirectoryName(fullPath)!;
        files.AddRange(Directory.GetFiles(containingFolder, Path.GetFileName(fullPath) + ".*").Where(p => char.IsDigit(p[^1])));
        //files.Reverse();

        var mainFile = files.Take(1).Select(File.ReadAllBytes).First();
        var ddsFiles = files.Skip(1).Reverse().Select(File.ReadAllBytes).ToList();

        //todo glossmap header

        if (!mainFile.AsSpan().StartsWith("DDS "u8))
            throw new ArgumentException("File is not a DDS file");
        var headerLength = BitConverter.ToInt32(mainFile, 4) + 4;

        if (mainFile.Length >= 88 && mainFile.AsSpan(84, 4).SequenceEqual("DX10"u8))
            headerLength += 20;

        using var ms = new MemoryStream();

        ms.Write(mainFile, 0, headerLength);

        foreach (var ddsFile in ddsFiles)
        {
            ms.Write(ddsFile);
        }

        ms.Write(mainFile, headerLength, mainFile.Length - headerLength);
        var sum = ddsFiles.Sum(f => f.Length);
        var wtf = ms.Length;
        var aa = ms.ToArray();
        File.WriteAllBytes(fullPath + ".unsplit.dds", aa);

        var image = Pfimage.FromStream(new MemoryStream(aa));
        SKColorType colorType;

        var newData = image.Data;
        var newDataLen = image.DataLen;
        var stride = image.Stride;
        switch (image.Format)
        {
            case ImageFormat.Rgb8:
                colorType = SKColorType.Gray8;
                break;
            case ImageFormat.R5g6b5:
                // color channels still need to be swapped
                colorType = SKColorType.Rgb565;
                break;
            case ImageFormat.Rgba16:
                // color channels still need to be swapped
                colorType = SKColorType.Argb4444;
                break;
            case ImageFormat.Rgb24:
                // Skia has no 24bit pixels, so we upscale to 32bit
                var pixels = image.DataLen / 3;
                newDataLen = pixels * 4;
                newData = new byte[newDataLen];
                for (int i = 0; i < pixels; i++)
                {
                    newData[i * 4] = image.Data[i * 3];
                    newData[i * 4 + 1] = image.Data[i * 3 + 1];
                    newData[i * 4 + 2] = image.Data[i * 3 + 2];
                    newData[i * 4 + 3] = 255;
                }

                stride = image.Width * 4;
                colorType = SKColorType.Bgra8888;
                break;
            case ImageFormat.Rgba32:
                colorType = SKColorType.Bgra8888;
                break;
            default:
                throw new ArgumentException($"Skia unable to interpret pfim format: {image.Format}");
        }

        var imageInfo = new SKImageInfo(image.Width, image.Height, colorType);
        var handle = GCHandle.Alloc(newData, GCHandleType.Pinned);
        var ptr = Marshal.UnsafeAddrOfPinnedArrayElement(newData, 0);
        using (var data = SKData.Create(ptr, newDataLen, (address, context) => handle.Free()))
        using (var skImage = SKImage.FromPixels(imageInfo, data, stride))
        using (var bitmap = SKBitmap.FromImage(skImage))
        using (var fs = File.Create(Path.ChangeExtension(fullPath, ".png")))
        using (var wstream = new SKManagedWStream(fs))
        {
            var success = bitmap.Encode(wstream, SKEncodedImageFormat.Png, 80);
            Console.WriteLine(success ? "Image converted successfully" : "Image unsuccessful");
        }

        return null;
    }

    private static bool IsGlossMap(ReadOnlySpan<char> path)
    {
        return path.EndsWith("dds.a");
    }

    private static bool IsNormals(ReadOnlySpan<char> path)
    {
        //ddna.dds.n
        if (path.Length < 8) return false;

        return path.EndsWith("ddna.dds") || path.EndsWith("ddna.dds.n") || (char.IsDigit(path[^1]) && path[..^1].EndsWith("ddna.dds"));
    }
}