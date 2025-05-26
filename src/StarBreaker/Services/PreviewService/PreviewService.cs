using System.Text;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.Logging;
using StarBreaker.Common;
using StarBreaker.Dds;
using StarBreaker.Extensions;
using StarBreaker.P4k;
using StarBreaker.Screens;

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

    private static readonly string[] plaintextExtensions = [".cfg", ".xml", ".txt", ".json", "eco", ".ini"];
    private static readonly string[] ddsLodExtensions = [".dds"];
    private static readonly string[] bitmapExtensions = [".bmp", ".jpg", ".jpeg", ".png"];
    //, ".dds.1", ".dds.2", ".dds.3", ".dds.4", ".dds.5", ".dds.6", ".dds.7", ".dds.8", ".dds.9"];

    public PreviewService(IP4kService p4kService, ILogger<PreviewService> logger)
    {
        _p4KService = p4kService;
        _logger = logger;
    }

    public FilePreviewViewModel GetPreview(P4kFileNode selectedEntry)
    {
        //TODO: move this to a service?
        using var entryStream = _p4KService.P4KFileSystem.P4kFile.OpenStream(selectedEntry.P4KEntry);

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
            preview = new TextPreviewViewModel(c.ToString(), ".xml"); // CryXML converts to XML
        }
        else if (plaintextExtensions.Any(p => selectedEntry.GetName().EndsWith(p, StringComparison.InvariantCultureIgnoreCase)))
        {
            _logger.LogInformation("plaintextExtensions");
            preview = new TextPreviewViewModel(entryStream.ReadString(), fileExtension);
        }
        else if (ddsLodExtensions.Any(p => selectedEntry.GetName().EndsWith(p, StringComparison.InvariantCultureIgnoreCase)))
        {
            try
            {
                var ms = DdsFile.MergeToStream(selectedEntry.P4KEntry.Name, _p4KService.P4KFileSystem);
                var pngBytes = DdsFile.ConvertToPng(ms.ToArray());
                _logger.LogInformation("ddsLodExtensions");
                preview = new DdsPreviewViewModel(new Bitmap(pngBytes));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to convert DDS file: {FileName}", selectedEntry.P4KEntry.Name);
                preview = new TextPreviewViewModel($"Failed to preview DDS file: {ex.Message}", fileExtension);
            }
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
                preview = new TextPreviewViewModel(c.ToString(), ".xml");
            }
            else if (plaintextExtensions.Any(p => fileName.EndsWith(p, StringComparison.InvariantCultureIgnoreCase)))
            {
                _logger.LogInformation("plaintextExtensions from SOCPAK");
                workingStream.Position = 0; // Reset position
                preview = new TextPreviewViewModel(workingStream.ReadString(), fileExtension);
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
                        var pngBytes = DdsFile.ConvertToPng(ms.ToArray());
                        _logger.LogInformation("ddsLodExtensions from SOCPAK (merged)");
                        preview = new DdsPreviewViewModel(new Bitmap(pngBytes));
                    }
                    catch (Exception mergeEx)
                    {
                        _logger.LogDebug(mergeEx, "Failed to merge LOD levels for SOCPAK DDS, trying direct conversion: {FileName}", selectedEntry.P4KEntry.Name);
                        
                        // Fallback: try converting the raw DDS file directly
                        var pngBytes = DdsFile.ConvertToPng(fileBytes);
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
        var current = node.Parent;
        while (current != null)
        {
            if (current is P4kSocPakFileNode socPakFileNode)
                return socPakFileNode;
            current = current.Parent;
        }
        return null;
    }
}