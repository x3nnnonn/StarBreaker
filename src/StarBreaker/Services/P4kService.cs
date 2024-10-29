using Microsoft.Extensions.Logging;
using StarBreaker.P4k;

namespace StarBreaker.Services;

public interface IP4kService
{
    P4kFile P4kFile { get; }
    void OpenP4k(string path, IProgress<double> progress);
}

public class P4kService : IP4kService
{
    private readonly ILogger<P4kService> _logger;
    private P4kFile? _p4KFile;

    public P4kFile P4kFile => _p4KFile ?? throw new InvalidOperationException("P4k file not open");

    public P4kService(ILogger<P4kService> logger)
    {
        _logger = logger;
    }

    public void OpenP4k(string path, IProgress<double> progress)
    {
        if (_p4KFile != null)
        {
            _logger.LogWarning("P4k file already open");
            return;
        }
        _p4KFile = P4kFile.FromFile(path, progress);
    }
}