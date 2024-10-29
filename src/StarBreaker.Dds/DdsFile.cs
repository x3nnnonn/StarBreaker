using System.Runtime.InteropServices;
using Pfim;
using SkiaSharp;
using StarBreaker.Common;

namespace StarBreaker.Dds;

public class DdsFile
{
    private readonly IImage _image;

    public DdsFile(IImage image)
    {
        _image = image;
    }

    public static DdsFile FromFile(string fullPath)
    {
        if (!fullPath.EndsWith(".dds", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("File must be a DDS file");

        var containingFolder = Path.GetDirectoryName(fullPath)!;
        var files = Directory.GetFiles(containingFolder, Path.GetFileName(fullPath) + ".*").Where(p => char.IsDigit(p[^1]));

        var mainFile = new BinaryReader(File.OpenRead(fullPath));

        //todo glossmap header

        if (mainFile.ReadUInt32() != MemoryMarshal.Read<uint>("DDS "u8))
            throw new ArgumentException("File is not a DDS file");

        var headerLength = 4 + mainFile.ReadUInt32();

        if (mainFile.BaseStream.Length >= 88 &&
            mainFile.BaseStream.Seek(84, SeekOrigin.Begin) == 84 &&
            "DX10"u8.SequenceEqual(mainFile.ReadBytes(4)))
            headerLength += 20;

        mainFile.BaseStream.Position = 0;
        
        using var ms = new MemoryStream();

        mainFile.BaseStream.CopyAmountTo(ms, (int)headerLength);

        foreach (var ddsFile in files.OrderDescending())
        {
            using var mipMapStream = new FileStream(ddsFile, FileMode.Open, FileAccess.Read);
            mipMapStream.CopyTo(ms);
        }

        mainFile.BaseStream.CopyAmountTo(ms, (int)(mainFile.BaseStream.Length - headerLength));

        ms.Position = 0;
        return new DdsFile(Pfimage.FromStream(ms));
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

    public SKBitmap ToSkia()
    {
        SKColorType colorType;

        var newData = _image.Data;
        var newDataLen = _image.DataLen;
        var stride = _image.Stride;
        switch (_image.Format)
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
                var pixels = _image.DataLen / 3;
                newDataLen = pixels * 4;
                newData = new byte[newDataLen];
                for (int i = 0; i < pixels; i++)
                {
                    newData[i * 4] = _image.Data[i * 3];
                    newData[i * 4 + 1] = _image.Data[i * 3 + 1];
                    newData[i * 4 + 2] = _image.Data[i * 3 + 2];
                    newData[i * 4 + 3] = 255;
                }

                stride = _image.Width * 4;
                colorType = SKColorType.Bgra8888;
                break;
            case ImageFormat.Rgba32:
                colorType = SKColorType.Bgra8888;
                break;
            case ImageFormat.R5g5b5:
            case ImageFormat.R5g5b5a1:
            default:
                throw new ArgumentException($"Skia unable to interpret pfim format: {_image.Format}");
        }

        var imageInfo = new SKImageInfo(_image.Width, _image.Height, colorType);
        var handle = GCHandle.Alloc(newData, GCHandleType.Pinned);
        var ptr = Marshal.UnsafeAddrOfPinnedArrayElement(newData, 0);
        using var data = SKData.Create(ptr, newDataLen, (address, context) => handle.Free());
        using var skImage = SKImage.FromPixels(imageInfo, data, stride);
        var bitmap = SKBitmap.FromImage(skImage);

        return bitmap;
    }

    public bool SaveAsPng(string fullPath)
    {
        var bitmap = ToSkia();
        using var fs = File.Create(Path.ChangeExtension(fullPath, ".png"));
        using var wstream = new SKManagedWStream(fs);
        return bitmap.Encode(wstream, SKEncodedImageFormat.Png, 80);
    }
}