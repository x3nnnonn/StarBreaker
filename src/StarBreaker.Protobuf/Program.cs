using StarBreaker.Protobuf;

const string starCitizenExe = @"C:\Program Files\Roberts Space Industries\StarCitizen\LIVE\Bin64\StarCitizen.exe";
const string sourcePath = "../../../../StarBreaker.Grpc/protos";

string[] whitelist =
[
    @"google\api\annotations.proto",
    @"google\api\http.proto",
    @"google\rpc\error_details.proto",
    @"google\rpc\status.proto",
    @"protoc-gen-openapiv2\options\annotations.proto",
    @"protoc-gen-openapiv2\options\openapiv2.proto",
];

var filesToDelete = Directory.EnumerateFiles(Path.GetFullPath(sourcePath), "*.proto", SearchOption.AllDirectories)
    .Where(x => !whitelist.Any(y => x.EndsWith(y, StringComparison.InvariantCultureIgnoreCase)));
foreach (var filePath in filesToDelete)
{
    File.Delete(filePath);
}

new ReadFromBinary(starCitizenExe).Generate(Path.GetFullPath(sourcePath));