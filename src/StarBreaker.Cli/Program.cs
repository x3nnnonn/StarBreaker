using CliFx;
using StarBreaker.Cli;

return await new CliApplicationBuilder()
    .SetExecutableName("StarBreaker.Cli")
    .AddCommand<DownloadCommand>()
    .AddCommand<ExportAllCommand>()
    .AddCommand<ImportAllCommand>()
    .AddCommand<ProcessAllCommand>()
    .AddCommand<ProcessCommand>()
    .AddCommand<WatchExportCommand>()
    .AddCommand<WatchImportCommand>()
    .AddCommand<DataCoreExtractCommand>()
    .AddCommand<ExtractP4kCommand>()
    .AddCommand<ExtractProtobufsCommand>()
    .AddCommand<ExtractDescriptorSetCommand>()
    .AddCommand<ConvertCryXmlBCommand>()
    .AddCommand<ConvertAllCryXmlBCommand>()
    .AddCommand<MergeDdsCommand>()
    .AddCommand<MergeAllDdsCommand>()
    .Build()
    .RunAsync();