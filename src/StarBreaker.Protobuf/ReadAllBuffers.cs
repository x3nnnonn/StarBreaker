using Google.Protobuf;
using StarBreaker.Grpc;

namespace StarBreaker.Protobuf;


/// <summary>
/// Attempts to read all grpc buffers captured by the proxy,
/// finding the protobuf type and writing it to json.
/// </summary>
public class ReadAllBuffers
{
    public static void Run()
    {
        const string folder = @"C:\Development\StarBreaker\scripts\dump";
        
        var files = Directory.GetFiles(folder, "*.grpc", SearchOption.AllDirectories);
        var processed = files.Select(f =>
        {
            var parts = Path.GetFileNameWithoutExtension(f).Split('-');
            if (parts.Length != 3)
                throw new Exception($"Invalid filename: {f}");
            return new
            {
                Filename = f,
                Index = int.Parse(parts[0]),
                RequestOrResponse = parts[1],
                Method = parts[2],
            };
        });
        
        var methods = processed.DistinctBy(p => p.Method).Select(p => p.Method).OrderBy(m => m).ToList();

        var types = typeof(GrpcUtils).Assembly.GetTypes().ToDictionary(t => t.FullName.ToLower());

        var formatter = new JsonFormatter(JsonFormatter.Settings.Default.WithIndentation());
        foreach (var entry in processed)
        {
            var name = entry.Method;
            var parts = name.Split('.');
            var namespaceName = string.Join(".", parts.Take(parts.Length - 1));
            var methodName = parts.Last();
            var lookup = $"{namespaceName}+{parts[^2]}client".Replace("_", "").ToLower();
            var type = types[lookup];
            var yes = type.GetMethods().Where(m => m.Name == methodName).OrderBy(m => m.GetParameters().Length).First();

            if (yes.ReturnParameter.ToString().Contains("Streaming"))
            {
                Console.WriteLine("Skipping streaming method");
                continue;
            }
            
            var requestType = entry.RequestOrResponse == "request" ? yes.GetParameters()[0].ParameterType :
                yes.ReturnType;
            
            
            var parser = requestType.GetProperty("Parser").GetValue(null) as MessageParser;

            var buffer = parser.ParseFrom(GrpcUtils.GrpcToProtobuf(File.ReadAllBytes(entry.Filename)));

            var json = formatter.Format(buffer);
            File.WriteAllText(entry.Filename + ".json", json);
            Console.WriteLine(requestType);
        }
    }
}