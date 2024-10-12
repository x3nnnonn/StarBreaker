using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace StarBreaker.Protobuf.Generator;

public class ReadFromBinary
{
    private readonly string _file;

    public ReadFromBinary(string file)
    {
        if (!File.Exists(file))
            throw new FileNotFoundException("File not found", file);

        _file = file;
    }
    
    private static bool IsRelevant(FileDescriptor f)
    {
        //sc and cig are fairly obvious, the other one seems to be arena commander statistics of some kind? a bit odd that it's different.
        return f.Package.StartsWith("sc", StringComparison.InvariantCultureIgnoreCase) ||
               f.Package.StartsWith("cig", StringComparison.InvariantCultureIgnoreCase) ||
               string.Equals(f.Package, "service_statistics", StringComparison.InvariantCultureIgnoreCase);
    }

    public void Generate(string protoPath)
    {
        var protos = GetDescriptorsFromFile();
        var fileDescriptors = FromFileDescriptorProtos(protos);
        var targetFolder = Directory.CreateDirectory(protoPath);
        
        foreach (var fileDescriptor in fileDescriptors.Where(IsRelevant))
        {
            var path = Path.Combine(targetFolder.FullName, fileDescriptor.Name);
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir)) Directory.CreateDirectory(dir);
            var protoText = fileDescriptor.ToProtoString(new DynamicGrpcPrinterOptions()
            {
                FullyQualified = true
            });
            File.WriteAllText(path, protoText);
        }
    }

    private List<FileDescriptorProto> GetDescriptorsFromFile()
    {
        var protos = new List<FileDescriptorProto>();
        var span = File.ReadAllBytes(_file).AsSpan();

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

            var varInt = (int)span.DecodeVarInt(start + 1, out var bytesRead);
            if (cursor - (int)bytesRead != varInt)
                continue;

            ReadOnlySpan<byte> tags = [0x12, 0x1a, 0x22, 0x2a, 0x32, 0x3a, 0x42, 0x4a, 0x50, 0x58, 0x62];
            if (tags.IndexOf(span[cursor]) == -1)
                continue;

            while (cursor < fileLength && tags.IndexOf(span[cursor]) != -1)
            {
                tags = tags[tags.IndexOf(span[cursor])..];

                var varInt2 = span.DecodeVarInt(cursor + 1, out var bytesRead2);
                var something = (span[cursor] & 0b111) == 2;
                cursor = (int)bytesRead2 + (something ? (int)varInt2 : 0);
            }

            protos.Add(FileDescriptorProto.Parser.ParseFrom(span[start..cursor]));
        }

        return protos;
    }

    public static IReadOnlyList<FileDescriptor> FromFileDescriptorProtos(IEnumerable<FileDescriptorProto> protos)
    {
        // Step 1 - FileDescriptor.BuildFromByteStrings doesn't support passing files in random order, so we need to reorder them with protos
        // It is very unfortunate, as we are doubling the deserialization of FileDescriptorProto
        var resolved = new HashSet<string>();
        var orderedList = new List<ByteString>();

        var unorderedList = protos.GroupBy(x => x.Name).Select(x =>
        {
            var count = x.Count();
            return count switch
            {
                0 => throw new InvalidOperationException("This should never happen"),
                1 => x.First(),
                _ => x.MaxBy(y => y.ToByteArray().Length)!
            };
        }).ToList();

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
            orderedList.Add(proto.ToByteString());
        }

        // Step 2 - Build FileDescriptor from properly ordered list
        return FileDescriptor.BuildFromByteStrings(orderedList);
    }
}