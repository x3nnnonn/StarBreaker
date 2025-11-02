using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace StarBreaker.Services;

public interface ITagDatabaseService
{
    Dictionary<string, string> GetTagNameCache();
    void LoadTagDatabase(string tagDatabaseXmlPath);
    string? ResolveTagName(string recordId);
}

public class TagDatabaseService : ITagDatabaseService
{
    private readonly ILogger<TagDatabaseService> _logger;
    private readonly ConcurrentDictionary<string, string> _tagCache = new();
    private bool _isLoaded;

    public TagDatabaseService(ILogger<TagDatabaseService> logger)
    {
        _logger = logger;
    }

    public Dictionary<string, string> GetTagNameCache()
    {
        return new Dictionary<string, string>(_tagCache);
    }

    public void LoadTagDatabase(string tagDatabaseXmlPath)
    {
        if (_isLoaded)
        {
            _logger.LogDebug("TagDatabase already loaded, skipping reload");
            return;
        }

        if (!File.Exists(tagDatabaseXmlPath))
        {
            _logger.LogWarning("TagDatabase file not found: {Path}", tagDatabaseXmlPath);
            return;
        }

        try
        {
            _logger.LogInformation("Loading TagDatabase from: {Path}", tagDatabaseXmlPath);
            var doc = XDocument.Load(tagDatabaseXmlPath);
            
            // Parse all Tag records and extract RecordId -> tagName mappings
            var tagElements = doc.Descendants("Record")
                .Where(r => r.Attribute("__type")?.Value == "Tag")
                .ToList();

            foreach (var tagElement in tagElements)
            {
                var recordId = tagElement.Attribute("__guid")?.Value;
                var tagName = tagElement.Attribute("tagName")?.Value;

                if (recordId != null && tagName != null)
                {
                    _tagCache[recordId.ToLowerInvariant()] = tagName;
                }
            }

            _isLoaded = true;
            _logger.LogInformation("Loaded {Count} tags from TagDatabase", _tagCache.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load TagDatabase from: {Path}", tagDatabaseXmlPath);
        }
    }

    public string? ResolveTagName(string recordId)
    {
        return _tagCache.TryGetValue(recordId.ToLowerInvariant(), out var tagName) ? tagName : null;
    }
}

