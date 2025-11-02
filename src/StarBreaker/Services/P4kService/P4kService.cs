using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using StarBreaker.Common;
using StarBreaker.CryXmlB;
using StarBreaker.FileSystem;
using StarBreaker.P4k;

namespace StarBreaker.Services;

public class P4kService : IP4kService
{
    private readonly ILogger<P4kService> _logger;
    private readonly ITagDatabaseService _tagDatabaseService;
    private P4kDirectoryNode? _p4KFile;

    public P4kDirectoryNode P4KFileSystem => _p4KFile ?? throw new InvalidOperationException("P4k file not open");

    public P4kService(ILogger<P4kService> logger, ITagDatabaseService tagDatabaseService)
    {
        _logger = logger;
        _tagDatabaseService = tagDatabaseService;
    }

    public void OpenP4k(string path, IProgress<double> p4kProgress, IProgress<double> fileSystemProgress)
    {
        if (_p4KFile != null)
        {
            _logger.LogWarning("P4k file already open");
            return;
        }

        var p4kFile = P4kFile.FromFile(path, p4kProgress);
        _p4KFile = P4kDirectoryNode.FromP4k(p4kFile, fileSystemProgress);
        
        // Try to load TagDatabase
        try
        {
            _logger.LogInformation("Searching for TagDatabase in P4K");
            var tagDbEntry = p4kFile.Entries.FirstOrDefault(e => 
                e.Name.Contains("TagDatabase.TagDatabase.xml", StringComparison.OrdinalIgnoreCase));
            
            if (tagDbEntry != null)
            {
                _logger.LogInformation("Found TagDatabase entry: {EntryName}", tagDbEntry.Name);
                using var entryStream = p4kFile.OpenStream(tagDbEntry);
                var ms = new MemoryStream();
                entryStream.CopyTo(ms);
                ms.Position = 0;
                
                if (CryXmlB.CryXml.IsCryXmlB(ms))
                {
                    _logger.LogInformation("TagDatabase is CryXmlB format, converting to XML");
                    ms.Position = 0;
                    if (CryXmlB.CryXml.TryOpen(ms, out var cryXml))
                    {
                        var tempPath = Path.Combine(Path.GetTempPath(), "StarBreaker_TagDatabase.xml");
                        File.WriteAllText(tempPath, cryXml.ToString());
                        _logger.LogInformation("Extracted TagDatabase to temp file: {TempPath}", tempPath);
                        _tagDatabaseService.LoadTagDatabase(tempPath);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to open CryXmlB TagDatabase");
                    }
                }
                else
                {
                    _logger.LogInformation("TagDatabase is not CryXmlB format, saving as plain XML");
                    var tempPath = Path.Combine(Path.GetTempPath(), "StarBreaker_TagDatabase.xml");
                    ms.Position = 0;
                    File.WriteAllText(tempPath, ms.ReadString());
                    _logger.LogInformation("Extracted TagDatabase to temp file: {TempPath}", tempPath);
                    _tagDatabaseService.LoadTagDatabase(tempPath);
                }
            }
            else
            {
                _logger.LogWarning("TagDatabase entry not found in P4K");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load TagDatabase from P4K");
        }
    }
}