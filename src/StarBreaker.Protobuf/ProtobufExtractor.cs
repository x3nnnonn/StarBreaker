using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace StarBreaker.Protobuf;

public class ProtobufExtractor
{
    public List<FileDescriptorProto> FileDescriptorProtos { get; }

    public ProtobufExtractor(string file)
    {
        if (!File.Exists(file))
            throw new FileNotFoundException("File not found", file);

        // first we get the raw file descriptors as decoded bytes from the exe
        var rawFileDescriptorProtos = GetDescriptorsFromFile(file);

        // some protos are duplicates (google well known types), so we deduplicate them. Keeps the largest one.
        var dedupedFileDescriptorProtos = DeduplicateFileDescriptors(rawFileDescriptorProtos);

        // we need them to be in order of dependency for a valid FileDescriptorSet. Do it here to guarantee valid output.
        FileDescriptorProtos = OrderByDependency(dedupedFileDescriptorProtos);
    }

    public void WriteProtos(string protoPath, Func<FileDescriptor, bool>? filter = null)
    {
        // This next step is kind of stupid, but it's the only format FileDescriptor accepts
        var protoByteStrings = FileDescriptorProtos.Select(x => x.ToByteString());
        var fileDescriptors = FileDescriptor.BuildFromByteStrings(protoByteStrings);

        var targetFolder = Directory.CreateDirectory(protoPath);

        var opts = new DynamicGrpcPrinterOptions
        {
            FullyQualified = true
        };
        
        foreach (var fileDescriptor in fileDescriptors)
        {
            if (filter != null && !filter(fileDescriptor))
                continue;
            
            var path = Path.Combine(targetFolder.FullName, fileDescriptor.Name);
            var dir = Path.GetDirectoryName(path);

            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(path, fileDescriptor.ToProtoString(opts));
        }
    }

    public void WriteDescriptorSet(string descriptorSetPath)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(descriptorSetPath));
        if (directory == null)
            throw new InvalidOperationException("Directory must be specified for descriptor set.");
        
        Directory.CreateDirectory(directory);

        var set = new FileDescriptorSet();
        set.File.AddRange(FileDescriptorProtos);

        using var output = File.Create(descriptorSetPath);
        set.WriteTo(output);
    }

    private static IEnumerable<FileDescriptorProto> DeduplicateFileDescriptors(IEnumerable<FileDescriptorProto> protos)
    {
        return protos
            .GroupBy(x => x.Name)
            .Select(x =>
                x.MaxBy(y => y.CalculateSize()) ?? throw new InvalidOperationException("MaxBy returned null")
            );
    }

    private static List<FileDescriptorProto> OrderByDependency(IEnumerable<FileDescriptorProto> unordered)
    {
        var resolved = new HashSet<string>();
        var orderedList = new List<FileDescriptorProto>();
        var unorderedList = new List<FileDescriptorProto>(unordered);

        while (unorderedList.Count > 0)
        {
            var proto = unorderedList.FirstOrDefault(x => x.Dependency.All(dep => resolved.Contains(dep)));
            if (proto == null)
            {
                throw new InvalidOperationException(
                    $"Invalid proto dependencies. Unable to resolve remaining protos [{string.Join(",", unorderedList.Select(x => x.Name))}] that don't have all their dependencies available.");
            }

            resolved.Add(proto.Name);
            unorderedList.Remove(proto);
            orderedList.Add(proto);
        }

        return orderedList;
    }

    private static List<FileDescriptorProto> GetDescriptorsFromFile(string file)
    {
        var protos = new List<FileDescriptorProto>();
        var span = File.ReadAllBytes(file).AsSpan();

        var fileLength = span.Length;

        int cursor = 0;
        while (cursor < fileLength)
        {
            var headerIndex = span[cursor..].IndexOf(".proto"u8);
            if (headerIndex == -1)
                break;

            cursor += headerIndex + ".proto"u8.Length;

            //.proto and .protodevel are both fine
            if (span[cursor..].StartsWith("devel"u8))
                cursor += "devel"u8.Length;

            var startIndex = span[int.Max(0, cursor - 1024).. cursor].LastIndexOf([(byte)0x0A]);
            if (startIndex == -1)
                continue;

            var start = startIndex + cursor - 1024;
            if (span[start - 1] == 0x0A && 0x0A == cursor - start - 1)
                start -= 1;

            var varInt = (int)DecodeVarInt(span, start + 1, out var bytesRead);
            if (cursor - (int)bytesRead != varInt)
                continue;

            ReadOnlySpan<byte> tags = [0x12, 0x1a, 0x22, 0x2a, 0x32, 0x3a, 0x42, 0x4a, 0x50, 0x58, 0x62];
            if (tags.IndexOf(span[cursor]) == -1)
                continue;

            while (cursor < fileLength && tags.IndexOf(span[cursor]) != -1)
            {
                tags = tags[tags.IndexOf(span[cursor])..];

                var varInt2 = DecodeVarInt(span, cursor + 1, out var bytesRead2);
                var something = (span[cursor] & 0b111) == 2;
                cursor = (int)bytesRead2 + (something ? (int)varInt2 : 0);
            }

            protos.Add(FileDescriptorProto.Parser.ParseFrom(span[start..cursor]));
        }

        return protos;
    }

    private static ulong DecodeVarInt(Span<byte> span, int pos, out ulong outPos)
    {
        var result = 0;
        var shift = 0;
        while (true)
        {
            var b = span[pos];
            result |= (b & 0x7f) << shift;
            pos += 1;
            if ((b & 0x80) == 0)
            {
                outPos = (ulong)pos;
                return (ulong)result;
            }
            shift += 7;
            if (shift >= 64)
                throw new Exception("Too many bytes when decoding varint.");
        }
    }
}