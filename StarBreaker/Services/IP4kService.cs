using System;
using StarBreaker.P4k;

namespace StarBreaker.Services;

public interface IP4kService
{
    P4kFile? P4k { get; }
    void LoadP4k(string path);
}