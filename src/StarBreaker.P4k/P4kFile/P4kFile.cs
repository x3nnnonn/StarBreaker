using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;
using StarBreaker.Common;
using ZstdSharp;

namespace StarBreaker.P4k;

public sealed class P4kFile : IP4kFile
{
    [ThreadStatic] private static Decompressor? decompressor;
    private readonly Aes _aes;

    public string P4KPath { get; }
    public P4kEntry[] Entries { get; }
    public P4kDirectoryNode Root { get; }

    private P4kFile(string path, P4kEntry[] entries, P4kDirectoryNode root)
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
        using var reader = new BinaryReader(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096), Encoding.UTF8, false);

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

        var entries = new P4kEntry[eocd64.TotalEntries];

        //use a channel so we can read entries and build the file system in parallel
        var channel = Channel.CreateUnbounded<P4kEntry>();

        var channelInsertTask = Task.Run(async () =>
        {
            var fileSystem = new P4kDirectoryNode("Root", null!);

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

    private static P4kEntry ReadEntry(BinaryReader reader)
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

            return new P4kEntry(
                fileName,
                compressedSize,
                uncompressedSize,
                header.CompressionMethod,
                isCrypted,
                localHeaderOffset,
                header.LastModifiedTimeAndDate,
                header.Crc32
            );
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rent);
        }
    }

    // Represents the raw stream from the p4k file, before any decryption or decompression
    private StreamSegment OpenInternal(P4kEntry entry)
    {
        var p4kStream = new FileStream(P4KPath, FileMode.Open, FileAccess.Read, FileShare.Read);

        p4kStream.Seek((long)entry.Offset, SeekOrigin.Begin);
        if (p4kStream.Read<uint>() is not 0x14034B50 and not 0x04034B50)
            throw new Exception("Invalid local file header");

        var localHeader = p4kStream.Read<LocalFileHeader>();

        var offset = entry.Offset
                     + sizeof(uint)
                     + (ulong)Unsafe.SizeOf<LocalFileHeader>()
                     + localHeader.FileNameLength
                     + localHeader.ExtraFieldLength;
        var length = entry.CompressedSize;

        return new StreamSegment(p4kStream, (long)offset, (long)length, false);
    }

    // Remarks: Streams returned by this method might not support seeking or length.
    // If these are required, consider using OpenInMemory instead.
    public Stream OpenStream(P4kEntry entry)
    {
        var entryStream = OpenInternal(entry);

        return entry switch
        {
            { IsCrypted: true, CompressionMethod: 100 } => Decompress(Decrypt(entryStream), entry.UncompressedSize),
            { IsCrypted: false, CompressionMethod: 100 } => Decompress(entryStream, entry.UncompressedSize),
            { IsCrypted: false, CompressionMethod: 0 } when entry.CompressedSize != entry.UncompressedSize => throw new Exception("Invalid stored file"),
            { IsCrypted: false, CompressionMethod: 0 } => OpenInternal(entry),
            _ => throw new Exception("Invalid compression method")
        };
    }

    private MemoryStream Decrypt(Stream entryStream)
    {
        var innerArray = new byte[entryStream.Length];
        var ms = new MemoryStream(innerArray);

        using (var transform = _aes.CreateDecryptor())
        using (var crypto = new CryptoStream(entryStream, transform, CryptoStreamMode.Read))
            crypto.CopyTo(ms);

        //trim the stream to the last non-null byte
        var lastNonNull = innerArray.AsSpan().LastIndexOfAnyExcept((byte)0);
        ms.SetLength(lastNonNull + 1);
        ms.Position = 0;

        return ms;
    }

    private static MemoryStream Decompress(Stream entryStream, ulong uncompressedSize)
    {
        var ms = new MemoryStream((int)uncompressedSize);

        using (var decompressionStream = new DecompressionStream(entryStream, decompressor: decompressor ??= new Decompressor(), leaveOpen: false))
            decompressionStream.CopyTo(ms);

        ms.SetLength(ms.Position);
        ms.Position = 0;
        return ms;
    }
}