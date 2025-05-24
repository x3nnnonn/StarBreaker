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
}

public class PreviewService : IPreviewService
{
    private readonly ILogger<PreviewService> _logger;
    private readonly IP4kService _p4KService;

    private static readonly string[] plaintextExtensions = [".cfg", ".xml", ".txt", ".json", "eco"];
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
        using var entryStream = _p4KService.P4KFileSystem.P4kFile.OpenStream(selectedEntry.ZipEntry);

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
                var ms = DdsFile.MergeToStream(selectedEntry.ZipEntry.Name, _p4KService.P4KFileSystem);
                var pngBytes = DdsFile.ConvertToPng(ms.ToArray());
                _logger.LogInformation("ddsLodExtensions");
                preview = new DdsPreviewViewModel(new Bitmap(pngBytes));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to convert DDS file: {FileName}", selectedEntry.ZipEntry.Name);
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
                _logger.LogError(ex, "Failed to load bitmap: {FileName}", selectedEntry.ZipEntry.Name);
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
                _logger.LogError(ex, "Failed to create hex preview: {FileName}", selectedEntry.ZipEntry.Name);
                preview = new TextPreviewViewModel($"Failed to create hex preview: {ex.Message}", fileExtension);
            }
        }
        //todo other types

        return preview;
    }
}