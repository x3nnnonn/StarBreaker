using System.Diagnostics.CodeAnalysis;
using CliFx;
using StarBreaker.Cli;

static class Program
{
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DownloadCommand))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DiffCommand))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ExportAllCommand))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ImportAllCommand))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ProcessAllCommand))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ProcessCommand))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(WatchExportCommand))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(WatchImportCommand))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DataCoreExtractCommand))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DataCoreTypeGeneratorCommand))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ExtractP4kCommand))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DumpP4kCommand))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ExtractProtobufsCommand))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ExtractDescriptorSetCommand))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ConvertCryXmlBCommand))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ConvertAllCryXmlBCommand))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MergeDdsCommand))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MergeAllDdsCommand))]
    
    public static async Task<int> Main()
    {
        return await new CliApplicationBuilder()
            .SetExecutableName("StarBreaker.Cli")
            .AddCommand<DownloadCommand>()
            .AddCommand<DiffCommand>()
            .AddCommand<ExportAllCommand>()
            .AddCommand<ImportAllCommand>()
            .AddCommand<ProcessAllCommand>()
            .AddCommand<ProcessCommand>()
            .AddCommand<WatchExportCommand>()
            .AddCommand<WatchImportCommand>()
            .AddCommand<DataCoreExtractCommand>()
            .AddCommand<DataCoreTypeGeneratorCommand>()
            .AddCommand<ExtractP4kCommand>()
            .AddCommand<DumpP4kCommand>()
            .AddCommand<ExtractProtobufsCommand>()
            .AddCommand<ExtractDescriptorSetCommand>()
            .AddCommand<ConvertCryXmlBCommand>()
            .AddCommand<ConvertAllCryXmlBCommand>()
            .AddCommand<MergeDdsCommand>()
            .AddCommand<MergeAllDdsCommand>()
            .Build()
            .RunAsync();
    }
}