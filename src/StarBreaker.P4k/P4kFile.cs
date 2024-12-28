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

        var eocdLocation = reader.Locate(EOCDRecord.Magic);
        reader.BaseStream.Seek(eocdLocation, SeekOrigin.Begin);
        var eocd = reader.Read<EOCDRecord>();
        var comment = reader.ReadBytes(eocd.CommentLength).AsSpan();

        if (!comment.StartsWith("CIG"u8))
            throw new Exception("Invalid comment");

        if (!eocd.IsZip64)
            throw new Exception("Not a zip64 archive");

        var bytesFromEnd = reader.BaseStream.Length - eocdLocation;
        var zip64LocatorLocation = reader.Locate(Zip64Locator.Magic, bytesFromEnd);
        reader.BaseStream.Seek(zip64LocatorLocation, SeekOrigin.Begin);
        var zip64Locator = reader.Read<Zip64Locator>();

        reader.BaseStream.Seek((long)zip64Locator.Zip64EOCDOffset, SeekOrigin.Begin);

        var eocd64 = reader.Read<EOCD64Record>();
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
        var header = reader.Read<CentralDirectoryFileHeader>();
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
                header.LastModifiedTime,
                header.LastModifiedDate
            );
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rent);
        }
    }

    public void Extract(string outputDir, string? filter = null, IProgress<double>? progress = null)
    {
        var filteredEntries = (filter is null
            ? Entries
            : Entries.Where(entry => FileSystemName.MatchesSimpleExpression(filter, entry.Name))).ToArray();

        var numberOfEntries = filteredEntries.Length;
        var fivePercent = numberOfEntries / 20;
        var processedEntries = 0;

        progress?.Report(0);

        var lockObject = new Lock();

        //TODO: Preprocessing step:
        // 1. start with the list of total files
        // 2. run the following according to the filter:
        // 3. find one-shot single file procesors
        // 4. find file -> multiple file processors
        // 5. find multiple file -> single file unsplit processors - remove from the list so we don't double process
        // run it!
        Parallel.ForEach(filteredEntries, entry =>
            {
                if (entry.UncompressedSize == 0)
                    return;

                var entryPath = Path.Combine(outputDir, entry.Name);
                if (File.Exists(entryPath))
                    return;

                Directory.CreateDirectory(Path.GetDirectoryName(entryPath) ?? throw new InvalidOperationException());
                var sss = (int)entry.UncompressedSize;
                using (var writeStream = new FileStream(entryPath, FileMode.Create, FileAccess.Write, FileShare.None,
                           bufferSize: entry.UncompressedSize > int.MaxValue ? 81920 : (int)entry.UncompressedSize, useAsync: true))
                {
                    using (var entryStream = Open(entry))
                    {
                        entryStream.CopyTo(writeStream);
                    }
                }

                Interlocked.Increment(ref processedEntries);
                if (processedEntries == numberOfEntries || processedEntries % fivePercent == 0)
                {
                    using (lockObject.EnterScope())
                    {
                        progress?.Report(processedEntries / (double)numberOfEntries);
                    }
                }
            }
        );
    }

    public Stream Open(ZipEntry entry)
    {
        var p4kStream = new FileStream(P4KPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: (int)entry.CompressedSize, useAsync: false);

        p4kStream.Seek((long)entry.Offset, SeekOrigin.Begin);
        if (p4kStream.Read<uint>() is not 0x14034B50 and not 0x04034B50)
            throw new Exception("Invalid local file header");

        var localHeader = p4kStream.Read<LocalFileHeader>();

        p4kStream.Seek(localHeader.FileNameLength + localHeader.ExtraFieldLength, SeekOrigin.Current);
        Stream entryStream = new StreamSegment(p4kStream, p4kStream.Position, (long)entry.CompressedSize, false);

        return entry switch
        {
            { IsCrypted: true, CompressionMethod: 100 } => GetDecryptStream(entryStream, entry.UncompressedSize),
            { IsCrypted: false, CompressionMethod: 100 } => GetDecompressionStream(entryStream, entry.UncompressedSize),
            { IsCrypted: false, CompressionMethod: 0 } when entry.CompressedSize != entry.UncompressedSize => throw new Exception("Invalid stored file"),
            { IsCrypted: false, CompressionMethod: 0 } => entryStream,
            _ => throw new Exception("Invalid compression method")
        };
    }

    private MemoryStream GetDecryptStream(Stream entryStream, ulong uncompressedSize)
    {
        using var transform = _aes.CreateDecryptor();

        var rent = ArrayPool<byte>.Shared.Rent((int)entryStream.Length);
        try
        {
            using var rented = new MemoryStream(rent, 0, (int)entryStream.Length, true, true);
            using (var crypto = new CryptoStream(entryStream, transform, CryptoStreamMode.Read))
            {
                crypto.CopyTo(rented);
            }

            // Trim NULL off end of stream
            rented.Seek(-1, SeekOrigin.End);
            while (rented.Position > 1 && rented.ReadByte() == 0)
                rented.Seek(-2, SeekOrigin.Current);
            rented.SetLength(rented.Position);

            rented.Seek(0, SeekOrigin.Begin);

            using var decompressionStream = new DecompressionStream(rented, leaveOpen: true);
            var finalStream = new MemoryStream((int)uncompressedSize);
            decompressionStream.CopyTo(finalStream);

            finalStream.Seek(0, SeekOrigin.Begin);

            return finalStream;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rent);
        }
    }

    private static MemoryStream GetDecompressionStream(Stream entryStream, ulong uncompressedSize)
    {
        // We need to make a new memoryStream and copy the data over.
        // This is because the decompression stream doesn't support seeking/position/length.

        var buffer = new MemoryStream((int)uncompressedSize);

        //close the entryStream (p4k file probably) when we're done with it
        using (var decompressionStream = new DecompressionStream(entryStream, leaveOpen: false))
            decompressionStream.CopyTo(buffer);

        buffer.Seek(0, SeekOrigin.Begin);

        return buffer;
    }

    public static List<ZipExtraField> ReadExtraFields(BinaryReader br, ushort length)
    {
        var fields = new List<ZipExtraField>();

        while (length > 0)
        {
            var tag = br.ReadUInt16();
            var size = br.ReadUInt16();
            var data = br.ReadBytes(size - 4);

            fields.Add(new ZipExtraField(tag, size, data));
            length -= size;
        }

        return fields;
    }
}
