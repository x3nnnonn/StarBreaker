using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.Logging;
using StarBreaker.Common;
using StarBreaker.CryChunkFile;
using StarBreaker.CryXmlB;
using StarBreaker.Dds;
using StarBreaker.Extensions;
using StarBreaker.P4k;
using StarBreaker.Screens;
using TextMateSharp.Internal.Rules;

namespace StarBreaker.Services;

public interface IPreviewService
{
    FilePreviewViewModel GetPreview(P4kFileNode clickedNode);
    FilePreviewViewModel GetSocPakFilePreview(P4kSocPakChildFileNode clickedNode);
}

public class PreviewService : IPreviewService
{
    private readonly ILogger<PreviewService> _logger;
    private readonly IP4kService _p4KService;
    private readonly ITagDatabaseService _tagDatabaseService;

    private static readonly string[] plaintextExtensions = [".cfg", ".xml", ".txt", ".json", "eco", ".ini"];
    private static readonly string[] ddsLodExtensions = [".dds"];
    private static readonly string[] bitmapExtensions = [".bmp", ".jpg", ".jpeg", ".png"];
    //, ".dds.1", ".dds.2", ".dds.3", ".dds.4", ".dds.5", ".dds.6", ".dds.7", ".dds.8", ".dds.9"];

    public PreviewService(IP4kService p4kService, ITagDatabaseService tagDatabaseService, ILogger<PreviewService> logger)
    {
        _p4KService = p4kService;
        _tagDatabaseService = tagDatabaseService;
        _logger = logger;
    }

    public FilePreviewViewModel GetPreview(P4kFileNode selectedEntry)
    {
        //TODO: move this to a service?
        using var entryStream = selectedEntry.P4k.OpenStream(selectedEntry.P4KEntry);

        FilePreviewViewModel preview;
        var fileName = selectedEntry.GetName();
        var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();

        //check cryxml before extension since ".xml" sometimes is cxml sometimes plaintext
        if (CryXmlB.CryXml.IsCryXmlB(entryStream))
        {
            if (!CryXmlB.CryXml.TryOpen(entryStream, out var c))
            {
                //should never happen
                _logger.LogError("Failed to open CryXmlB");
                return new TextPreviewViewModel("Failed to open CryXmlB", fileExtension);
            }

            _logger.LogInformation("cryxml");
            var xmlText = c.ToString();
            xmlText = ResolveXmlTags(xmlText);
            preview = new TextPreviewViewModel(xmlText, ".xml"); // CryXML converts to XML
        }
        else if (fileName.EndsWith(".soc", StringComparison.InvariantCultureIgnoreCase))
        {
            using var ms = new MemoryStream();
            entryStream.CopyTo(ms);
            var socBytes = ms.ToArray();
            preview = CreateSocPreview(socBytes);
        }
        else if (plaintextExtensions.Any(p => selectedEntry.GetName().EndsWith(p, StringComparison.InvariantCultureIgnoreCase)))
        {
            _logger.LogInformation("plaintextExtensions");
            var text = entryStream.ReadString();
            if (fileExtension == ".xml")
            {
                text = ResolveXmlTags(text);
            }
            preview = new TextPreviewViewModel(text, fileExtension);
        }
        else if (ddsLodExtensions.Any(p => selectedEntry.GetName().EndsWith(p, StringComparison.InvariantCultureIgnoreCase)))
        {
            var ms = DdsFile.MergeToStream(selectedEntry.P4KEntry.Name, selectedEntry.Root.RootNode);
            var pngBytes = DdsFile.ConvertToPng(ms.ToArray(), true, true);
            _logger.LogInformation("ddsLodExtensions");
            preview = new DdsPreviewViewModel(new Bitmap(pngBytes));
        }
        else if (bitmapExtensions.Any(p => selectedEntry.GetName().EndsWith(p, StringComparison.InvariantCultureIgnoreCase)))
        {
            _logger.LogInformation("bitmapExtensions");
            try
            {
                preview = new DdsPreviewViewModel(new Bitmap(entryStream));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load bitmap: {FileName}", selectedEntry.P4KEntry.Name);
                preview = new TextPreviewViewModel($"Failed to preview bitmap: {ex.Message}", fileExtension);
            }
        }
        else
        {
            _logger.LogInformation("hex");
            try
            {
                preview = new HexPreviewViewModel(entryStream.ToArray());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create hex preview: {FileName}", selectedEntry.P4KEntry.Name);
                preview = new TextPreviewViewModel($"Failed to create hex preview: {ex.Message}", fileExtension);
            }
        }
        //todo other types

        return preview;
    }

    public FilePreviewViewModel GetSocPakFilePreview(P4kSocPakChildFileNode selectedEntry)
    {
        try
        {
            // Find the parent SOCPAK file node to get the P4K file instance
            var socPakFileNode = FindParentSocPakFileNode(selectedEntry);
            if (socPakFileNode == null)
            {
                _logger.LogError("Could not find parent SOCPAK file node");
                return new TextPreviewViewModel("Could not find parent SOCPAK file");
            }

            // Get the cached SOCPAK file instance
            var socPakFile = socPakFileNode.SocPakFile;
            if (socPakFile == null)
            {
                _logger.LogError("Could not load SOCPAK file: {FileName}", socPakFileNode.P4KEntry.Name);
                return new TextPreviewViewModel("Could not load SOCPAK file");
            }

                        // Open the specific file from the SOCPAK and copy to memory stream
            byte[] fileBytes;
            try
            {
                using var entryStream = socPakFile.OpenStream(selectedEntry.P4KEntry);
                using var memoryStream = new MemoryStream();
                entryStream.CopyTo(memoryStream);
                fileBytes = memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read file from SOCPAK: {FileName}", selectedEntry.P4KEntry.Name);
                return new TextPreviewViewModel($"Failed to read file from SOCPAK: {ex.Message}");
            }

            FilePreviewViewModel preview;
            var fileName = selectedEntry.GetName();
            var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();

            // Create a new memory stream for each operation
            using var workingStream = new MemoryStream(fileBytes);

            // Use the same preview logic as the main preview service
            if (CryXmlB.CryXml.IsCryXmlB(workingStream))
            {
                workingStream.Position = 0; // Reset position
                if (!CryXmlB.CryXml.TryOpen(workingStream, out var c))
                {
                    _logger.LogError("Failed to open CryXmlB from SOCPAK");
                    return new TextPreviewViewModel("Failed to open CryXmlB", fileExtension);
                }

                _logger.LogInformation("cryxml from SOCPAK");
                var socpakXmlText = c.ToString();
                socpakXmlText = ResolveXmlTags(socpakXmlText);
                preview = new TextPreviewViewModel(socpakXmlText, ".xml");
            }
            else if (fileName.EndsWith(".soc", StringComparison.InvariantCultureIgnoreCase))
            {
                preview = CreateSocPreview(fileBytes);
            }
            else if (plaintextExtensions.Any(p => fileName.EndsWith(p, StringComparison.InvariantCultureIgnoreCase)))
            {
                _logger.LogInformation("plaintextExtensions from SOCPAK");
                workingStream.Position = 0; // Reset position
                var text = workingStream.ReadString();
                if (fileExtension == ".xml")
                {
                    text = ResolveXmlTags(text);
                }
                preview = new TextPreviewViewModel(text, fileExtension);
            }
            else if (ddsLodExtensions.Any(p => fileName.EndsWith(p, StringComparison.InvariantCultureIgnoreCase)))
            {
                try
                {
                    // For SOCPAK files, try to use the merged LOD approach first
                    try
                    {
                        var socPakFileSystem = new P4kFileSystem(socPakFile);
                        var ms = DdsFile.MergeToStream(selectedEntry.P4KEntry.Name, socPakFileSystem);
                        var pngBytes = DdsFile.ConvertToPng(ms.ToArray(), true, true);
                        _logger.LogInformation("ddsLodExtensions from SOCPAK (merged)");
                        preview = new DdsPreviewViewModel(new Bitmap(pngBytes));
                    }
                    catch (Exception mergeEx)
                    {
                        _logger.LogDebug(mergeEx, "Failed to merge LOD levels for SOCPAK DDS, trying direct conversion: {FileName}", selectedEntry.P4KEntry.Name);
                        
                        // Fallback: try converting the raw DDS file directly
                        var pngBytes = DdsFile.ConvertToPng(fileBytes, true, true);
                        _logger.LogInformation("ddsLodExtensions from SOCPAK (direct)");
                        preview = new DdsPreviewViewModel(new Bitmap(pngBytes));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to convert DDS file from SOCPAK: {FileName}", selectedEntry.P4KEntry.Name);
                    preview = new TextPreviewViewModel($"Failed to preview DDS file: {ex.Message}", fileExtension);
                }
            }
            else if (bitmapExtensions.Any(p => fileName.EndsWith(p, StringComparison.InvariantCultureIgnoreCase)))
            {
                _logger.LogInformation("bitmapExtensions from SOCPAK");
                try
                {
                    workingStream.Position = 0; // Reset position
                    preview = new DdsPreviewViewModel(new Bitmap(workingStream));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load bitmap from SOCPAK: {FileName}", selectedEntry.P4KEntry.Name);
                    preview = new TextPreviewViewModel($"Failed to preview bitmap: {ex.Message}", fileExtension);
                }
            }
            else
            {
                _logger.LogInformation("hex from SOCPAK");
                try
                {
                    preview = new HexPreviewViewModel(fileBytes);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create hex preview from SOCPAK: {FileName}", selectedEntry.P4KEntry.Name);
                    preview = new TextPreviewViewModel($"Failed to create hex preview: {ex.Message}", fileExtension);
                }
            }

            return preview;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to preview SOCPAK file: {FileName}", selectedEntry.P4KEntry.Name);
            return new TextPreviewViewModel($"Failed to preview SOCPAK file: {ex.Message}");
        }
    }

    private static P4kSocPakFileNode? FindParentSocPakFileNode(IP4kNode node)
    {
        IP4kNode? current = node;
        while (true)
        {
            if (current is P4kSocPakFileNode socPakFileNode)
                return socPakFileNode;
            else if (current is P4kSocPakChildFileNode child)
                current = child.Parent;
            else if (current is P4kSocPakDirectoryNode dir)
                current = dir.Parent;
            else
                return null;
        }
    }

    private string ResolveXmlTags(string xml)
    {
        try
        {
            // Use regex to find all Tag/tag RecordId attributes and replace them with comments showing the tag name
            var regex = new Regex(
                @"<[Tt]ag[^>]*RecordId=""([a-fA-F0-9\-]+)""[^>]*/?>",
                RegexOptions.IgnoreCase);

            return regex.Replace(xml, match =>
            {
                var recordId = match.Groups[1].Value;
                var tagName = _tagDatabaseService.ResolveTagName(recordId);
                
                if (tagName != null)
                {
                    // Add a comment before the Tag element showing the tag name
                    return $"<!-- Tag: {tagName} -->{match.Value}";
                }
                
                return match.Value;
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve XML tags");
            return xml;
        }
    }

    private FilePreviewViewModel CreateSocPreview(byte[] socBytes)
    {
        try
        {
            if (!CrChFile.TryRead(socBytes, out var socFile))
            {
                return new TextPreviewViewModel("Unable to parse .soc file", ".txt");
            }

            var sb = new StringBuilder();
            sb.AppendLine($"SOC chunk count: {socFile.Headers.Length}");
            sb.AppendLine();

            for (int i = 0; i < socFile.Headers.Length; i++)
            {
                var header = socFile.Headers[i];
                sb.AppendLine($"[{i}] Type: {header.ChunkType} (0x{((ushort)header.ChunkType):X4}), Version: {header.Version}, Size: {header.Size} bytes");
            }

            string? firstXml = null;
            foreach (var chunk in socFile.Chunks)
            {
                if (CryXml.IsCryXmlB(chunk))
                {
                    using var chunkStream = new MemoryStream(chunk);
                    var cryXml = new CryXml(chunkStream);
                    firstXml = ResolveXmlTags(cryXml.ToString());
                    break;
                }
            }

            if (firstXml != null)
            {
                sb.AppendLine();
                sb.AppendLine("First CryXML chunk:");
                sb.AppendLine(firstXml);
                return new TextPreviewViewModel(sb.ToString(), ".xml");
            }

            return new TextPreviewViewModel(sb.ToString(), ".txt");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create SOC preview");
            return new TextPreviewViewModel($"Failed to preview SOC file: {ex.Message}");
        }
    }
}