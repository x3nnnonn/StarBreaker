using System.Runtime.InteropServices;
using Pfim;
using StarBreaker.Common;

namespace StarBreaker.Dds;

public static class DdsFile
{
    public static Stream MergeToStream(string fullPath)
    {
        if (!fullPath.EndsWith(".dds", StringComparison.OrdinalIgnoreCase) && !fullPath.EndsWith(".dds.a", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("File must be a DDS file");

        var containingFolder = Path.GetDirectoryName(fullPath)!;
        var files = Directory.GetFiles(containingFolder, Path.GetFileName(fullPath) + ".*").Where(p => char.IsDigit(p[^1]));

        var mainFile = new BinaryReader(File.OpenRead(fullPath));

        if (mainFile.ReadUInt32() != MemoryMarshal.Read<uint>("DDS "u8))
            throw new ArgumentException("File is not a DDS file");

        var headerLength = 4 + mainFile.ReadUInt32();

        if (mainFile.BaseStream.Length >= 88 &&
            mainFile.BaseStream.Seek(84, SeekOrigin.Begin) == 84 &&
            "DX10"u8.SequenceEqual(mainFile.ReadBytes(4)))
            headerLength += 20;

        mainFile.BaseStream.Position = 0;

        var ms = new MemoryStream();

        //todo glossmap header

        mainFile.BaseStream.CopyAmountTo(ms, (int)headerLength);

        //order by the number at the end. e.g. 8 is the largest, 0 is the smallest.
        // we want to write the largest mipmap first.
        foreach (var ddsFile in files.OrderDescending())
        {
            using var mipMapStream = new FileStream(ddsFile, FileMode.Open, FileAccess.Read);
            mipMapStream.CopyTo(ms);
        }

        mainFile.BaseStream.CopyAmountTo(ms, (int)(mainFile.BaseStream.Length - headerLength));

        ms.Position = 0;
        return ms;
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
}