using System.Buffers;
using DirectXTexNet;
using StarBreaker.Common;

namespace StarBreaker.Dds;

public static class DdsFile
{
    public static ReadOnlySpan<byte> Magic => "DDS "u8;

    public static Stream MergeToStream(string fullPath)
    {
        if (!fullPath.EndsWith(".dds", StringComparison.OrdinalIgnoreCase) && !fullPath.EndsWith(".dds.a", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("File must be a DDS file");

        var containingFolder = Path.GetDirectoryName(fullPath)!;
        var files = Directory.GetFiles(containingFolder, Path.GetFileName(fullPath) + ".*").Where(p => char.IsDigit(p[^1]));

        var mainFile = new BinaryReader(File.OpenRead(fullPath));

        var magic = mainFile.ReadBytes(4);
        if (!Magic.SequenceEqual(magic))
            throw new ArgumentException("File is not a DDS file");

        var header = mainFile.BaseStream.Read<DdsHeader>();
        bool readDx10Header = false;
        DdsHeaderDxt10 headerDx10 = default;
        if (header.PixelFormat.FourCC == CompressionAlgorithm.DX10)
        {
            headerDx10 = mainFile.BaseStream.Read<DdsHeaderDxt10>();
            readDx10Header = true;
            //if (headerDx10.DxgiFormat != DXGI_FORMAT.BC6H_UF16)
            //    throw new ArgumentException("File is not a BC6H_UF16 DDS file");
        }

        var smallMipMapBytes = mainFile.BaseStream.ReadArray<byte>((int)(mainFile.BaseStream.Length - mainFile.BaseStream.Position));
        var mipMapSizes = MipMapSizes(header);

        var finalDds = new MemoryStream();

        //todo glossmap header

        finalDds.Write(Magic);
        finalDds.Write(header);
        if (readDx10Header)
            finalDds.Write(headerDx10);

        //order by the number at the end. e.g. 8 is the largest, 0 is the smallest.
        // we want to write the largest mipmap first.
        var mipMapFiles = files.OrderDescending().Select(File.ReadAllBytes).ToArray();

        //DDS_SURFACE_FLAGS_CUBEMAP
        var faces = (header.Caps & 0x8) != 0 ? 6 : 1;
        // var faces = headerDx10.ResourceDimension == ResourceDimension.D3D10_RESOURCE_DIMENSION_TEXTURE3D ? 6 : 1;
        var smallOffset = 0;
        for (var cubeFace = 0; cubeFace < faces; cubeFace++)
        {
            for (var mipMap = 0; mipMap < mipMapSizes.Length; mipMap++)
            {
                var mipMapSize = mipMapSizes[mipMap];
                var mipMapByteCount = GetMipmapSize(mipMapSize.Item1, mipMapSize.Item2);

                if (mipMap < mipMapFiles.Length)
                {
                    //If we have a dedicated file for this mipmap, use it.
                    finalDds.Write(mipMapFiles[mipMap], cubeFace * mipMapByteCount, mipMapByteCount);
                }
                else
                {
                    //Otherwise, use the bytes at the end of the main file.
                    finalDds.Write(smallMipMapBytes, smallOffset, mipMapByteCount);
                    smallOffset += mipMapByteCount;
                }
            }
        }

        finalDds.Position = 0;
        return finalDds;
    }

    private static (int, int)[] MipMapSizes(DdsHeader header)
    {
        var mipMapSizes = new (int, int)[header.MipMapCount];

        for (var i = 0; i < header.MipMapCount; i++)
        {
            var width = Math.Max((int)(header.Width / Math.Pow(2, i)), 1);
            var height = Math.Max((int)(header.Height / Math.Pow(2, i)), 1);
            mipMapSizes[i] = (width, height);
        }

        return mipMapSizes;
    }

    private static int GetMipmapSize(int width, int height)
    {
        return Math.Max(1, (width + 3) / 4) * Math.Max(1, (height + 3) / 4) * 16;
    }

    public static void MergeToFile(string ddsFullPath, string pngFullPath)
    {
        using var s = MergeToStream(ddsFullPath);
        using var fs = new FileStream(pngFullPath, FileMode.Create, FileAccess.Write, FileShare.None, (int)s.Length, false);
        s.CopyTo(fs);
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

    public static void ConvertToPng(string ddsFullPath, string pngFullPath)
    {
        var tex = TexHelper.Instance.LoadFromDDSFile(ddsFullPath, DDS_FLAGS.NONE);
        var meta = tex.GetMetadata();

        if (TexHelper.Instance.IsTypeless(meta.Format, false))
        {
            Console.WriteLine(" ");
        }

        if (TexHelper.Instance.IsPlanar(meta.Format))
        {
            Console.WriteLine(" ");
        }

        if (TexHelper.Instance.IsCompressed(meta.Format))
        {
            var decompressed = tex.Decompress(DXGI_FORMAT.UNKNOWN);
            tex.Dispose();
            tex = decompressed;
        }

        if (meta.Format != DXGI_FORMAT.R8G8B8A8_UNORM)
        {
            var converted = tex.Convert(DXGI_FORMAT.R8G8B8A8_UNORM, TEX_FILTER_FLAGS.DEFAULT, 0.5f);
            tex.Dispose();
            tex = converted;
        }

        var count = tex.GetImageCount();

        if (count == 0)
            throw new InvalidOperationException("No images found in DDS file");

        var pathWithoutExtension = Path.GetFileNameWithoutExtension(ddsFullPath);
        Directory.CreateDirectory(Path.GetDirectoryName(pngFullPath)!);

        for (var i = 0; i < count; i++)
        {
            var path = Path.Combine(Path.GetDirectoryName(pngFullPath)!, $"{pathWithoutExtension}_{i}.jpg");
            tex.SaveToWICFile(i, WIC_FLAGS.NONE, TexHelper.Instance.GetWICCodec(WICCodecs.JPEG), path);
        }
    }
}