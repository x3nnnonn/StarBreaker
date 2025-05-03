using System.Buffers;
using System.Runtime.InteropServices;
using DirectXTexNet;
using StarBreaker.Common;
using StarBreaker.FileSystem;

namespace StarBreaker.Dds;

public static class DdsFile
{
    public static ReadOnlySpan<byte> Magic => "DDS "u8;

    public static Stream MergeToStream(string fullPath, IFileSystem? fileSystem = null)
    {
        fileSystem ??= RealFileSystem.Instance;
        if (!fullPath.EndsWith(".dds", StringComparison.OrdinalIgnoreCase) && !fullPath.EndsWith(".dds.a", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("File must be a DDS file");

        var containingFolder = Path.GetDirectoryName(fullPath)!;
        var files = fileSystem.EnumerateFiles(containingFolder, Path.GetFileName(fullPath) + ".*")
            .Where(p => char.IsDigit(p[^1]))
            .ToArray();

        var mainFile = new BinaryReader(fileSystem.OpenRead(fullPath));

        if (files.Length == 0)
        {
            var remaining = mainFile.BaseStream.Length - mainFile.BaseStream.Position;
            
        }

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
        var mipMapFiles = files.OrderDescending().Select(fileSystem.ReadAllBytes).ToArray();

        var largest = mipMapSizes[0];
        var largestByteCount = GetMipmapSize(largest.Item1, largest.Item2, header.PixelFormat, readDx10Header ? headerDx10 : null);

        if (mipMapFiles[0].Length % largestByteCount != 0)
            throw new ArgumentException("File is not a valid DDS file");

        var faces = mipMapFiles[0].Length / largestByteCount;
        var smallOffset = 0;
        for (var cubeFace = 0; cubeFace < faces; cubeFace++)
        {
            for (var mipMap = 0; mipMap < mipMapSizes.Length; mipMap++)
            {
                var mipMapSize = mipMapSizes[mipMap];
                var mipMapByteCount = GetMipmapSize(mipMapSize.Item1, mipMapSize.Item2, header.PixelFormat, readDx10Header ? headerDx10 : null);

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

    private static int GetMipmapSize(int width, int height, DdsPixelFormat format, DdsHeaderDxt10? dx10Header = null)
    {
        //TODO: is this even correct?
        var blockSize = format.FourCC switch
        {
            CompressionAlgorithm.D3DFMT_DXT1 => 8,
            CompressionAlgorithm.BC4S => 8,
            CompressionAlgorithm.BC4U => 8,
            _ => 16
        };
        if (dx10Header?.DxgiFormat is DXGI_FORMAT.BC4_SNORM or DXGI_FORMAT.BC4_UNORM or DXGI_FORMAT.BC4_TYPELESS)
        {
            blockSize = 8;
        }

        return Math.Max(1, (width + 3) / 4) * Math.Max(1, (height + 3) / 4) * blockSize;
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
            var bytes = tex.SaveToWICMemory(i, WIC_FLAGS.NONE, TexHelper.Instance.GetWICCodec(WICCodecs.PNG));
        }
    }

    public static unsafe MemoryStream ConvertToPng(byte[] dds)
    {
        ScratchImage? tex = null;
        fixed (byte* ptr = dds)
        {
            tex = TexHelper.Instance.LoadFromDDSMemory((IntPtr)ptr, dds.Length, DDS_FLAGS.NONE);
        }

        var meta = tex.GetMetadata();

        // if (!TexHelper.Instance.IsPlanar(meta.Format))
        // {
        //     var planar = tex.ConvertToSinglePlane();
        //     tex.Dispose();
        //     tex = planar;
        //     meta = tex.GetMetadata();
        // }

        if (TexHelper.Instance.IsCompressed(meta.Format))
        {
            var decompressed = tex.Decompress(DXGI_FORMAT.UNKNOWN);
            tex.Dispose();
            tex = decompressed;
            meta = tex.GetMetadata();
        }

        if (meta.Format != DXGI_FORMAT.R8G8B8A8_UNORM)
        {
            var converted = tex.Convert(0, DXGI_FORMAT.R8G8B8A8_UNORM, TEX_FILTER_FLAGS.DEFAULT, 0.5f);
            tex.Dispose();
            tex = converted;
            meta = tex.GetMetadata();
        }

        var count = tex.GetImageCount();

        if (count == 0)
            throw new InvalidOperationException("No images found in DDS file");
        var stream = new MemoryStream();

        using var bytes = tex.SaveToWICMemory(0, WIC_FLAGS.NONE, TexHelper.Instance.GetWICCodec(WICCodecs.PNG));

        //TODO: convert cubemap dds to a x-cross
        bytes.CopyTo(stream);

        stream.Position = 0;

        return stream;
    }
}