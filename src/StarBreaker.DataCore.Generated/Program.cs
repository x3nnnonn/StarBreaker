using StarBreaker.Common;
using StarBreaker.DataCore;
using StarBreaker.DataCoreGenerated;
using StarBreaker.P4k;

var timer = new TimeLogger();
var p4k = new P4kFileSystem(P4kFile.FromFile(@"C:\Program Files\Roberts Space Industries\StarCitizen\PTU\Data.p4k"));
var dcbStream = p4k.OpenRead(@"Data\Game2.dcb");

var df = new DataForge<DataCoreTypedRecord>(new DataCoreBinaryGenerated(new DataCoreDatabase(dcbStream)));
        
timer.LogReset("Loaded DataForge");

var allRecords = df.DataCore.Database.MainRecords
    .AsParallel()
    .Select(x => df.GetFromRecord(x))
    .ToList();
timer.LogReset("Extracted all records.");

var classDefinitions = allRecords.Where(r => r.Data is EntityClassDefinition).Select(r => r.Data as EntityClassDefinition).ToList();
//var spaceships = classDefinitions.Where(x => x.Data.tags.Any(t => t?.tagName == "Ship")).ToList();
        
Console.WriteLine();