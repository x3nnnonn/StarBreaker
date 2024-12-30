using System.Runtime.InteropServices;
using Pfim;
using SkiaSharp;

namespace StarBreaker.Dds;

public static class PfimExtensions
{
    public static SKBitmap ToSkia(this IImage _image)
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

    public static bool SaveAsPng(this IImage image, string fullPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        var bitmap = image.ToSkia();
        using var fs = File.Create(Path.ChangeExtension(fullPath, ".png"));
        using var wstream = new SKManagedWStream(fs);
        return bitmap.Encode(wstream, SKEncodedImageFormat.Png, 80);
    }
}