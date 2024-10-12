using StarBreaker.Protobuf;

const string starCitizenExe = @"C:\Program Files\Roberts Space Industries\StarCitizen\PTU\Bin64\StarCitizen.exe";
const string sourcePath = "../../../../StarBreaker.Grpc/protos";

new ReadFromBinary(starCitizenExe).Generate(Path.GetFullPath(sourcePath));