using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using StarBreaker.Common;
using ZstdSharp;

namespace StarBreaker.P4k;

public sealed class P4kFile
{
    public string P4KPath { get; }
    private readonly ZipEntry[] _entries;

    public ZipEntry[] Entries => _entries;

    public P4kFile(string filePath)
    {
        P4KPath = filePath;
        using var reader = new BinaryReader(new FileStream(P4KPath, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024), Encoding.UTF8, false);

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

        _entries = new ZipEntry[eocd64.EntriesOnDisk];

        reader.BaseStream.Seek((long)eocd64.CentralDirectoryOffset, SeekOrigin.Begin);
        
        for (var i = 0; i < (int)eocd64.TotalEntries; i++)
        {
            var header = reader.Read<CentralDirectoryFileHeader>();
            var length = header.FileNameLength + header.ExtraFieldLength + header.FileCommentLength;
            var rent = ArrayPool<byte>.Shared.Rent(length);
            try
            {
                var bytes = rent.AsSpan(0, length);

                if (reader.Read(bytes) != length)
                    throw new Exception();

                var reader2 = new SpanReader(bytes);

                var fileName = reader2.ReadStringOfLength(header.FileNameLength);
                ulong compressedSize = header.CompressedSize;
                ulong uncompressedSize = header.UncompressedSize;
                ulong localHeaderOffset = header.LocalFileHeaderOffset;
                ulong diskNumberStart = header.DiskNumberStart;

                if (reader2.ReadUInt16() != 1)
                    throw new Exception();

                var zip64HeaderSize = reader2.Read<ushort>();

                if (uncompressedSize == uint.MaxValue)
                    uncompressedSize = reader2.Read<ulong>();

                if (compressedSize == uint.MaxValue)
                    compressedSize = reader2.Read<ulong>();

                if (localHeaderOffset == uint.MaxValue)
                    localHeaderOffset = reader2.Read<ulong>();

                if (diskNumberStart == ushort.MaxValue)
                    diskNumberStart = reader2.Read<uint>();

                if (reader2.Read<ushort>() != 0x5000)
                    throw new Exception("Invalid extra field id");
                
                var extra0x5000Size = reader2.Read<ushort>();
                reader2.Advance(extra0x5000Size - 4);

                if (reader2.Read<ushort>() != 0x5002)
                    throw new Exception("Invalid extra field id");
                if (reader2.Read<ushort>() != 6)
                    throw new Exception("Invalid extra field size");
                
                var isCrypted = reader2.Read<ushort>() == 1;

                if (reader2.Read<ushort>() != 0x5003)
                    throw new Exception("Invalid extra field id");
                
                var extra0x5003Size = reader2.Read<ushort>();
                reader2.Advance(extra0x5003Size - 4);

                if (header.FileCommentLength != 0)
                    throw new Exception("File comment not supported");

                _entries[i] = new ZipEntry(
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
    }

    public void Extract(string outputDir, IProgress<double>? progress = null)
    {
        var numberOfEntries = _entries.Length;
        var fivePercent = numberOfEntries / 20;
        var processedEntries = 0;

        progress?.Report(0);

        Parallel.ForEach(_entries, entry =>
            {
                using var p4kStream = new FileStream(P4KPath, FileMode.Open, FileAccess.Read, FileShare.Read);

                p4kStream.Seek((long)entry.Offset, SeekOrigin.Begin);

                if (p4kStream.Read<uint>() is not 0x14034B50 and not 0x04034B50)
                    throw new Exception("Invalid local file header");

                var header = p4kStream.Read<LocalFileHeader>();
                p4kStream.Seek(header.FileNameLength + header.ExtraFieldLength, SeekOrigin.Current);

                var entryPath = Path.Combine(outputDir, entry.Name);
                Directory.CreateDirectory(Path.GetDirectoryName(entryPath) ?? throw new InvalidOperationException());

                // part of the file stream that contains the compressed data
                Stream entryStream = new StreamSegment(p4kStream, p4kStream.Position, (long)entry.CompressedSize);

                // if the file is encrypted, decrypt it
                if (entry.IsCrypted)
                    entryStream = GetDecryptStream(entryStream);

                if (entry.CompressionMethod == 100)
                {
                    entryStream = new DecompressionStream(entryStream);
                }
                else if (entry.CompressionMethod == 0)
                {
                    if (entry.CompressedSize != entry.UncompressedSize)
                    {
                        throw new Exception("Invalid stored file");
                    }
                    //leave entryStream as is
                }
                else
                {
                    throw new Exception("Invalid compression method");
                }

                using var writeStream = new FileStream(entryPath, FileMode.Create, FileAccess.Write, FileShare.None);

                entryStream.CopyTo(writeStream);

                entryStream.Dispose();

                lock (_entries)
                {
                    processedEntries++;
                    if (processedEntries == numberOfEntries || processedEntries % fivePercent == 0)
                        progress?.Report(processedEntries / (double)numberOfEntries);
                }
            }
        );
    }

    private static readonly byte[] _key =
    [
        0x5E, 0x7A, 0x20, 0x02,
        0x30, 0x2E, 0xEB, 0x1A,
        0x3B, 0xB6, 0x17, 0xC3,
        0x0F, 0xDE, 0x1E, 0x47
    ];

    private static Stream GetDecryptStream(Stream entryStream)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = new byte[16];
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.None;

        var cipher = aes.CreateDecryptor();

        var crypto = new CryptoStream(entryStream, cipher, CryptoStreamMode.Read);
        //return crypto;

        var buffer = new MemoryStream();
        crypto.CopyTo(buffer);

        // Trim NULL off end of stream
        buffer.Seek(-1, SeekOrigin.End);
        while (buffer.Position > 1 && buffer.ReadByte() == 0) buffer.Seek(-2, SeekOrigin.Current);
        buffer.SetLength(buffer.Position);

        buffer.Seek(0, SeekOrigin.Begin);

        entryStream = buffer;
        return entryStream;
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