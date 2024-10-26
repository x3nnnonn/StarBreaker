using Microsoft.Extensions.Logging;
using StarBreaker.P4k;

namespace StarBreaker.Services;

public interface IP4kService
{
    P4kFile? P4kFile { get; }
    void OpenP4k(string path, IProgress<double> progress);
}

public class P4kService : IP4kService
{
    private readonly ILogger<P4kService> _logger;

    public P4kFile? P4kFile { get; private set; }

    public P4kService(ILogger<P4kService> logger)
    {
        _logger = logger;
    }

    public void OpenP4k(string path, IProgress<double> progress)
    {
        if (P4kFile != null)
        {
            _logger.LogWarning("P4k file already open");
            return;
        }
        P4kFile = P4kFile.FromFile(path, progress);
    }

    public int FileCount => P4kFile?.Entries.Length ?? 0;
}