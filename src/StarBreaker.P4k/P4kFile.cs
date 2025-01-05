using System.Buffers;
using System.IO.Enumeration;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;
using StarBreaker.Common;
using ZstdSharp;

namespace StarBreaker.P4k;

public sealed partial class P4kFile
{
    [ThreadStatic] private static Decompressor? decompressor;
    private readonly Aes _aes;

    public ZipEntry[] Entries { get; }
    public string P4KPath { get; }
    public ZipNode Root { get; }

    private P4kFile(string path, ZipEntry[] entries, ZipNode root)
    {
        P4KPath = path;
        Root = root;
        Entries = entries;

        _aes = Aes.Create();
        _aes.Mode = CipherMode.CBC;
        _aes.Padding = PaddingMode.Zeros;
        _aes.IV = new byte[16];
        _aes.Key =
        [
            0x5E, 0x7A, 0x20, 0x02,
            0x30, 0x2E, 0xEB, 0x1A,
            0x3B, 0xB6, 0x17, 0xC3,
            0x0F, 0xDE, 0x1E, 0x47
        ];
    }

    public static P4kFile FromFile(string filePath, IProgress<double>? progress = null)
    {
        progress?.Report(0);
        using var reader = new BinaryReader(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None, 4096), Encoding.UTF8, false);

        var eocdLocation = reader.BaseStream.Locate(EOCDRecord.Magic);
        reader.BaseStream.Seek(eocdLocation, SeekOrigin.Begin);
        var eocd = reader.BaseStream.Read<EOCDRecord>();
        var comment = reader.ReadBytes(eocd.CommentLength).AsSpan();

        if (!comment.StartsWith("CIG"u8))
            throw new Exception("Invalid comment");

        if (!eocd.IsZip64)
            throw new Exception("Not a zip64 archive");

        var bytesFromEnd = reader.BaseStream.Length - eocdLocation;
        var zip64LocatorLocation = reader.BaseStream.Locate(Zip64Locator.Magic, bytesFromEnd);
        reader.BaseStream.Seek(zip64LocatorLocation, SeekOrigin.Begin);
        var zip64Locator = reader.BaseStream.Read<Zip64Locator>();

        reader.BaseStream.Seek((long)zip64Locator.Zip64EOCDOffset, SeekOrigin.Begin);

        var eocd64 = reader.BaseStream.Read<EOCD64Record>();
        if (eocd64.Signature != BitConverter.ToUInt32(EOCD64Record.Magic))
            throw new Exception("Invalid zip64 end of central directory locator");

        var reportInterval = (int)Math.Max(eocd64.TotalEntries / 50, 1);
        reader.BaseStream.Seek((long)eocd64.CentralDirectoryOffset, SeekOrigin.Begin);

        var entries = new ZipEntry[eocd64.TotalEntries];

        //use a channel so we can read entries and build the file system in parallel
        var channel = Channel.CreateUnbounded<ZipEntry>();

        var channelInsertTask = Task.Run(async () =>
        {
            var fileSystem = new ZipNode("_root_");

            await foreach (var entry in channel.Reader.ReadAllAsync())
            {
                fileSystem.Insert(entry);
            }

            return fileSystem;
        });

        for (var i = 0; i < (int)eocd64.TotalEntries; i++)
        {
            var entry = ReadEntry(reader);
            entries[i] = entry;
            if (!channel.Writer.TryWrite(entry))
                throw new Exception("Failed to write to channel");

            if (i % reportInterval == 0)
                progress?.Report(i / (double)eocd64.TotalEntries);
        }

        channel.Writer.Complete();

        channelInsertTask.Wait();
        var fileSystem = channelInsertTask.Result;

        progress?.Report(1);
        return new P4kFile(filePath, entries, fileSystem);
    }

    private static ZipEntry ReadEntry(BinaryReader reader)
    {
        var header = reader.BaseStream.Read<CentralDirectoryFileHeader>();
        var length = header.FileNameLength + header.ExtraFieldLength + header.FileCommentLength;
        var rent = ArrayPool<byte>.Shared.Rent(length);
        try
        {
            var bytes = rent.AsSpan(0, length);

            reader.BaseStream.ReadExactly(bytes);

            var reader2 = new SpanReader(bytes);

            var fileName = reader2.ReadStringOfLength(header.FileNameLength);
            ulong compressedSize = header.CompressedSize;
            ulong uncompressedSize = header.UncompressedSize;
            ulong localHeaderOffset = header.LocalFileHeaderOffset;
            ulong diskNumberStart = header.DiskNumberStart;

            if (reader2.ReadUInt16() != 1)
                throw new Exception("Invalid version needed to extract");

            var zip64HeaderSize = reader2.ReadUInt16();

            if (uncompressedSize == uint.MaxValue)
                uncompressedSize = reader2.ReadUInt64();

            if (compressedSize == uint.MaxValue)
                compressedSize = reader2.ReadUInt64();

            if (localHeaderOffset == uint.MaxValue)
                localHeaderOffset = reader2.ReadUInt64();

            if (diskNumberStart == ushort.MaxValue)
                diskNumberStart = reader2.ReadUInt32();

            if (reader2.ReadUInt16() != 0x5000)
                throw new Exception("Invalid extra field id");

            var extra0x5000Size = reader2.ReadUInt16();
            reader2.Advance(extra0x5000Size - 4);

            if (reader2.ReadUInt16() != 0x5002)
                throw new Exception("Invalid extra field id");
            if (reader2.ReadUInt16() != 6)
                throw new Exception("Invalid extra field size");

            var isCrypted = reader2.ReadUInt16() == 1;

            if (reader2.ReadUInt16() != 0x5003)
                throw new Exception("Invalid extra field id");

            var extra0x5003Size = reader2.ReadUInt16();
            reader2.Advance(extra0x5003Size - 4);

            if (header.FileCommentLength != 0)
                throw new Exception("File comment not supported");

            return new ZipEntry(
                fileName,
                compressedSize,
                uncompressedSize,
                header.CompressionMethod,
                isCrypted,
                localHeaderOffset,
                header.LastModifiedTimeAndDate
            );
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rent);
        }
    }

    // Represents the raw stream from the p4k file, before any decryption or decompression
    private StreamSegment OpenInternal(ZipEntry entry)
    {
        var p4kStream = new FileStream(P4KPath, FileMode.Open, FileAccess.Read, FileShare.Read);

        p4kStream.Seek((long)entry.Offset, SeekOrigin.Begin);
        if (p4kStream.Read<uint>() is not 0x14034B50 and not 0x04034B50)
            throw new Exception("Invalid local file header");

        var localHeader = p4kStream.Read<LocalFileHeader>();

        p4kStream.Seek(localHeader.FileNameLength + localHeader.ExtraFieldLength, SeekOrigin.Current);

        return new StreamSegment(p4kStream, p4kStream.Position, (long)entry.CompressedSize, false);
    }

    // Remarks: Streams returned by this method might not support seeking or length.
    // If these are required, consider using OpenInMemory instead.
    public Stream OpenStream(ZipEntry entry)
    {
        // Represents the raw stream from the p4k file, before any decryption or decompression
        var entryStream = OpenInternal(entry);

        return entry switch
        {
            { IsCrypted: true, CompressionMethod: 100 } => GetDecryptStream(entryStream, entry.CompressedSize),
            { IsCrypted: false, CompressionMethod: 100 } => GetDecompressionStream(entryStream),
            { IsCrypted: false, CompressionMethod: 0 } when entry.CompressedSize != entry.UncompressedSize => throw new Exception("Invalid stored file"),
            { IsCrypted: false, CompressionMethod: 0 } => entryStream,
            _ => throw new Exception("Invalid compression method")
        };
    }

    private DecompressionStream GetDecryptStream(Stream entryStream, ulong compressedSize)
    {
        using var transform = _aes.CreateDecryptor();
        var ms = new MemoryStream((int)compressedSize);
        using (var crypto = new CryptoStream(entryStream, transform, CryptoStreamMode.Read))
            crypto.CopyTo(ms);

        // Trim NULL off end of stream
        ms.Seek(-1, SeekOrigin.End);
        while (ms.Position > 1 && ms.ReadByte() == 0)
            ms.Seek(-2, SeekOrigin.Current);
        ms.SetLength(ms.Position);

        ms.Seek(0, SeekOrigin.Begin);

        return GetDecompressionStream(ms);
    }

    private static DecompressionStream GetDecompressionStream(Stream entryStream)
    {
        return new DecompressionStream(entryStream, decompressor: decompressor ??= new Decompressor());
    }

    public byte[] OpenInMemory(ZipEntry entry)
    {
        if (entry.UncompressedSize > int.MaxValue)
            throw new Exception("File too large to load into memory. Use OpenStream instead");

        var uncompressedSize = checked((int)entry.UncompressedSize);

        var ms = new MemoryStream(uncompressedSize);
        OpenStream(entry).CopyTo(ms);

        // If the stream is larger than the uncompressed size, trim it.
        // This can happen because of decryption padding bytes :(
        ms.SetLength(ms.Position);

        return ms.ToArray();
    }
}