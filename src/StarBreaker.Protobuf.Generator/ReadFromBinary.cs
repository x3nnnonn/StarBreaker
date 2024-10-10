using Google.Protobuf.Reflection;
using ProtoBuf;
using ProtoBuf.Reflection;

namespace StarBreaker.Protobuf.Generator;

public class ReadFromBinary
{
    private readonly string _file;
    
     public ReadFromBinary(string file)
    {
        _file = file;
    }

    public void Generate(string protoPath)
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

            protos.Add(Serializer.Deserialize<FileDescriptorProto>(span[start..cursor]));
        }
        var set = new FileDescriptorSet();
        foreach (var proto in protos)
        {
            proto.IncludeInOutput = true;
            set.Files.Add(proto);
        }

        set.Process();
        
        var codeFiles = CSharpCodeGenerator.Default.Generate(set).ToArray();
        
        //TODO: this is pretty broken sadly :(
        //the upper part works though!
        foreach (var codeFile in codeFiles)
        {
            var path = Path.Combine(protoPath, codeFile.Name);
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(path, FixCodeFile(codeFile.Text));
        }
    }

    private static string FixCodeFile(string codeFile)
    {
        var lines = codeFile.Split(Environment.NewLine);
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line.Contains("public ."))
            {
                lines[i] = line.Replace("public .", "public ");
            }
            if (line.Contains("private ."))
            {
                lines[i] = line.Replace("private .", "private ");
            }
            if (line.Contains("List<."))
            {
                lines[i] = line.Replace("List<.", "List<");
            }
            if (line.Contains("(."))
            {
                lines[i] = line.Replace("(.", "(");
            }
            if (line.Contains(", ."))
            {
                lines[i] = line.Replace(", .", ", ");
            }
            if (line.Contains("sc.internal."))
            {
                lines[i] = line.Replace("sc.internal.", "sc.@internal.");
            }
            if (line.Contains("this ."))
            {
                lines[i] = line.Replace("this .", "this ");
            }
        }
        
        return string.Join("\n", lines);
    }
}