using System;
using StarBreaker.P4k;

namespace StarBreaker.Services;

public class P4KService : IP4kService
{
    public P4kFile? P4k { get; private set; }
    
    public void LoadP4k(string path)
    {
        P4k = new P4kFile(path);
    }
    
    public void Dispose()
    {
        P4k?.Dispose();
        GC.SuppressFinalize(this);
    }
}