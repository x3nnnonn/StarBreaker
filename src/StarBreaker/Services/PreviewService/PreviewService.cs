using System.Text;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.Logging;
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

    private static readonly string[] plaintextExtensions = [".cfg", ".xml", ".txt", ".json"];
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
        var buffer = _p4KService.P4KFileSystem.P4kFile.OpenInMemory(selectedEntry.ZipEntry);

        FilePreviewViewModel preview;

        //check cryxml before extension since ".xml" sometimes is cxml sometimes plaintext
        if (CryXmlB.CryXml.IsCryXmlB(buffer))
        {
            if (!CryXmlB.CryXml.TryOpen(new MemoryStream(buffer), out var c))
            {
                //should never happen
                _logger.LogError("Failed to open CryXmlB");
                return new TextPreviewViewModel("Failed to open CryXmlB");
            }

            _logger.LogInformation("cryxml");
            preview = new TextPreviewViewModel(c.ToString());
        }
        else if (plaintextExtensions.Any(p => selectedEntry.GetName().EndsWith(p, StringComparison.InvariantCultureIgnoreCase)))
        {
            _logger.LogInformation("plaintextExtensions");

            preview = new TextPreviewViewModel(Encoding.UTF8.GetString(buffer));
        }
        else if (ddsLodExtensions.Any(p => selectedEntry.GetName().EndsWith(p, StringComparison.InvariantCultureIgnoreCase)))
        {
            var parent = selectedEntry.Parent;
            if (parent == null)
            {
                _logger.LogError("ddsLodExtensions: parent is null");
                return new TextPreviewViewModel("ddsLodExtensions: parent is null");
            }

            var ms = DdsFile.MergeToStream(selectedEntry.ZipEntry.Name, _p4KService.P4KFileSystem);
            var pngBytes = DdsFile.ConvertToPng(ms.ToArray());
            //find all mipmaps of the dds.


            _logger.LogInformation("ddsLodExtensions");
            preview = new DdsPreviewViewModel(new Bitmap(pngBytes));
        }
        else if (bitmapExtensions.Any(p => selectedEntry.GetName().EndsWith(p, StringComparison.InvariantCultureIgnoreCase)))
        {
            var ms = new MemoryStream(buffer);
            _logger.LogInformation("bitmapExtensions");
            preview = new DdsPreviewViewModel(new Bitmap(ms));
        }
        else
        {
            _logger.LogInformation("hex");
            preview = new HexPreviewViewModel(buffer);
        }
        //todo other types

        return preview;
    }
}