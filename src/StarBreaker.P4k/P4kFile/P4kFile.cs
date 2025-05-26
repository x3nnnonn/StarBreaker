using System.Buffers;
using System.Diagnostics;
using System.IO.Compression;
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

        // The comment is always "CIG" in Data.p4k, but we don't enforce it to keep compatibility with other p4k files (zip and socpak).
        // if (!comment.StartsWith("CIG"u8))
        //     throw new Exception("Invalid comment");

        ulong centralDirectoryOffset;
        ulong totalEntries;

        if (eocd.IsZip64)
        {
            var bytesFromEnd = reader.BaseStream.Length - eocdLocation;
            var zip64LocatorLocation = reader.BaseStream.Locate(Zip64Locator.Magic, bytesFromEnd);
            reader.BaseStream.Seek(zip64LocatorLocation, SeekOrigin.Begin);
            var zip64Locator = reader.BaseStream.Read<Zip64Locator>();

            reader.BaseStream.Seek((long)zip64Locator.Zip64EOCDOffset, SeekOrigin.Begin);

            var eocd64 = reader.BaseStream.Read<EOCD64Record>();
            if (eocd64.Signature != BitConverter.ToUInt32(EOCD64Record.Magic))
                throw new Exception("Invalid zip64 end of central directory locator");
            centralDirectoryOffset = eocd64.CentralDirectoryOffset;
            totalEntries = eocd64.TotalEntries;
        }
        else
        {
            centralDirectoryOffset = eocd.CentralDirectoryOffset;
            totalEntries = eocd.TotalEntries;
        }


        var reportInterval = (int)Math.Max(totalEntries / 50, 1);
        reader.BaseStream.Seek((long)centralDirectoryOffset, SeekOrigin.Begin);

        var entries = new P4kEntry[totalEntries];

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

        for (var i = 0; i < (int)totalEntries; i++)
        {
            var entry = ReadEntry(reader, eocd.IsZip64);
            entries[i] = entry;
            if (!channel.Writer.TryWrite(entry))
                throw new Exception("Failed to write to channel");

            if (i % reportInterval == 0)
                progress?.Report(i / (double)totalEntries);
        }

        channel.Writer.Complete();

        channelInsertTask.Wait();
        var fileSystem = channelInsertTask.Result;

        // Create P4kFile instance first
        var p4kFile = new P4kFile(filePath, entries, fileSystem);
        
        // Transform SOCPAK files into expandable nodes
        fileSystem.TransformSocPakFiles(p4kFile);

        progress?.Report(1);
        return p4kFile;
    }

    private static P4kEntry ReadEntry(BinaryReader reader, bool isZip64)
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
            uint diskNumberStart = header.DiskNumberStart; // Keep as uint for non-zip64

            // Handle Zip64 extra field if present
            if (isZip64 && (uncompressedSize == uint.MaxValue || compressedSize == uint.MaxValue || localHeaderOffset == uint.MaxValue || diskNumberStart == ushort.MaxValue))
            {
                // It's possible that a file is marked as zip64 overall,
                // but individual entries might not need the extended fields if their values fit in the standard fields.
                // We should still check for the extra field marker.
                if (header.ExtraFieldLength > 0)
                {
                    // The Zip64 extended information extra field has the Header ID 0x0001.
                    // Search for it within the extra field data.
                    int extraFieldStartPos = reader2.Position;
                    bool foundZip64ExtraField = false;
                    while (reader2.Position < extraFieldStartPos + header.ExtraFieldLength)
                    {
                        ushort extraFieldHeaderId = reader2.ReadUInt16();
                        ushort extraFieldDataSize = reader2.ReadUInt16();
                        if (extraFieldHeaderId == 0x0001) // Zip64 extended information
                        {
                            if (uncompressedSize == uint.MaxValue)
                                uncompressedSize = reader2.ReadUInt64();
                            else if (extraFieldDataSize >= 8) // still advance if it was already set
                                reader2.Advance(sizeof(ulong));


                            if (compressedSize == uint.MaxValue)
                                compressedSize = reader2.ReadUInt64();
                            else if (extraFieldDataSize >= 16)
                                reader2.Advance(sizeof(ulong));

                            if (localHeaderOffset == uint.MaxValue)
                                localHeaderOffset = reader2.ReadUInt64();
                            else if (extraFieldDataSize >= 24)
                                reader2.Advance(sizeof(ulong));

                            if (diskNumberStart == ushort.MaxValue)
                                diskNumberStart = reader2.ReadUInt32();
                            // No need to advance for diskNumberStart as it's the last optional field in the minimal Zip64 extra field.

                            foundZip64ExtraField = true;
                            break; // Found the Zip64 extra field
                        }
                        else
                        {
                            // Skip other extra fields
                            reader2.Advance(extraFieldDataSize);
                        }
                    }

                    if (!foundZip64ExtraField && (uncompressedSize == uint.MaxValue || compressedSize == uint.MaxValue || localHeaderOffset == uint.MaxValue || diskNumberStart == ushort.MaxValue))
                    {
                        // This case should ideally not happen if the EOCD IsZip64 flag is set correctly
                        // and the archive is well-formed. It means some values indicate Zip64 but the field is missing.
                        // Depending on strictness, this could be an error.
                        // For now, we'll proceed with the potentially truncated values from the main header.
                    }
                }
            }


            // The rest of the p4k-specific extra field reading assumes it follows the Zip64 field or starts directly if no Zip64 field was needed/present.
            // This part needs careful adjustment based on the actual structure of these custom fields.
            // Assuming the custom fields (0x5000, 0x5002, 0x5003) always follow.

            // It's safer to re-evaluate the position for custom fields based on whether a zip64 field was processed.
            // However, the original code directly reads them. We'll try to maintain that logic but acknowledge it might be fragile
            // if the extra field layout isn't fixed.

            bool isCrypted = false; // Default to false

            // The original code assumed a fixed order after the Zip64 field.
            // A more robust way would be to iterate through extra fields by their IDs.
            // For now, let's try to adapt the existing logic.
            // We need to ensure reader2 is at the correct position to read 0x5000, 0x5002, etc.
            // This means if a Zip64 field was *not* fully read because some values were not maxed out,
            // reader2 might not be at the start of the next actual custom field.

            // To be more robust, it would be better to parse all extra fields by ID and size.
            // Simplified approach for now, assuming custom fields are next:

            int currentExtraFieldPosition = reader2.Position; // Position after filename and potential Zip64 field.
            int endOfExtraFields = header.FileNameLength + header.ExtraFieldLength;

            while (reader2.Position < endOfExtraFields)
            {
                if (reader2.Remaining < 4) break; // Not enough space for ID and Size
                ushort fieldId = reader2.Peek<ushort>();
                if (fieldId == 0x0001 && isZip64) // Already processed Zip64 above if needed
                {
                    reader2.Advance(2); // ID
                    ushort zip64FieldSize = reader2.ReadUInt16();
                    reader2.Advance(zip64FieldSize);
                    continue;
                }

                if (fieldId == 0x5000)
                {
                    reader2.Advance(2); // ID
                    ushort extra0x5000Size = reader2.ReadUInt16();
                    reader2.Advance(extra0x5000Size); // Assuming the original logic of skipping the content is correct
                }
                else if (fieldId == 0x5002)
                {
                    reader2.Advance(2); // ID
                    if (reader2.ReadUInt16() != 6) // Size check for 0x5002
                        throw new Exception("Invalid extra field size for 0x5002");
                    isCrypted = reader2.ReadUInt16() == 1;
                    reader2.Advance(4); // Skip the rest of the 0x5002 field (assuming 2 bytes for isCrypted and 4 unknown/padding)
                }
                else if (fieldId == 0x5003)
                {
                    reader2.Advance(2); // ID
                    ushort extra0x5003Size = reader2.ReadUInt16();
                    reader2.Advance(extra0x5003Size); // Assuming the original logic of skipping the content is correct
                }
                else
                {
                    // Unknown or already processed field, skip it
                    reader2.Advance(2); // ID
                    if (reader2.Remaining < 2) break;
                    ushort unknownFieldSize = reader2.ReadUInt16();
                    if (reader2.Remaining < unknownFieldSize) break;
                    reader2.Advance(unknownFieldSize);
                }
            }


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
            { IsCrypted: false, CompressionMethod: 0 } => entryStream,
            { IsCrypted: false, CompressionMethod: 8 } => Deflate(entryStream),
            _ => throw new Exception($"Invalid compression method or encryption state: CompressionMethod={entry.CompressionMethod}, IsCrypted={entry.IsCrypted} for entry {entry.Name}")
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
        // Ensure uncompressedSize is not excessively large before allocating MemoryStream
        if (uncompressedSize > int.MaxValue) // MemoryStream capacity is int
        {
            // Handle large files differently, e.g., stream to a temporary file or use a different stream type
            throw new NotSupportedException("File too large for in-memory decompression with MemoryStream.");
        }

        var ms = new MemoryStream((int)uncompressedSize);

        using (var decompressionStream = new DecompressionStream(entryStream, decompressor: decompressor ??= new Decompressor(), leaveOpen: false))
            decompressionStream.CopyTo(ms);

        ms.SetLength(ms.Position);
        ms.Position = 0;
        return ms;
    }

    private static DeflateStream Deflate(Stream entryStream)
    {
        // Use the built-in DeflateStream for deflate compression
        return new DeflateStream(entryStream, CompressionMode.Decompress, leaveOpen: false);
    }
}