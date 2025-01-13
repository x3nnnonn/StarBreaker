using StarBreaker.P4k;

namespace StarBreaker.Services;

public interface IP4kService
{
    P4kFileSystem P4KFileSystem { get; }
    void OpenP4k(string path, IProgress<double> progress);
}